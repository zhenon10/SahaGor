using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SahaGor.Application.Kimlik;
using SahaGor.Application.Kimlik.Dtolar;
using SahaGor.Application.Kimlik.Istisnalar;
using SahaGor.Domain.Varliklar;
using SahaGor.Infrastructure.Persistence;

namespace SahaGor.Infrastructure.Kimlik;

/// <summary>
/// SG-120/SG-121/SG-122 kabul kriterlerini karsilayan giris ve token yenileme is akisi:
/// kimlik dogrulama, brute-force kilitlemesi ve yenileme jetonu rotasyonu.
/// </summary>
public sealed class KimlikDogrulamaServisi : IKimlikDogrulamaServisi
{
    private readonly SahaGorDbContext _dbContext;
    private readonly ISifreHashleyici _sifreHashleyici;
    private readonly IJwtTokenUretici _jwtTokenUretici;
    private readonly ILogger<KimlikDogrulamaServisi> _logger;

    public KimlikDogrulamaServisi(
        SahaGorDbContext dbContext,
        ISifreHashleyici sifreHashleyici,
        IJwtTokenUretici jwtTokenUretici,
        ILogger<KimlikDogrulamaServisi> logger)
    {
        _dbContext = dbContext;
        _sifreHashleyici = sifreHashleyici;
        _jwtTokenUretici = jwtTokenUretici;
        _logger = logger;
    }

    public async Task<GirisYaniti> GirisYapAsync(GirisIstegi istek, CancellationToken iptalToken = default)
    {
        ArgumentNullException.ThrowIfNull(istek);

        var personel = await _dbContext.Personeller
            .FirstOrDefaultAsync(p => p.KullaniciAdi == istek.KullaniciAdi, iptalToken);

        if (personel is null)
        {
            _logger.LogWarning("Basarisiz giris denemesi: tanimsiz kullanici adi '{KullaniciAdi}'.", istek.KullaniciAdi);
            throw new GecersizGirisException();
        }

        if (personel.KilitliMi())
        {
            _logger.LogWarning("Kilitli hesapla giris denemesi: {KullaniciAdi}.", istek.KullaniciAdi);
            throw new HesapKilitliException(personel.KilitlenmeBitisZamaniUtc!.Value);
        }

        if (!personel.AktifMi)
        {
            _logger.LogWarning("Pasif hesapla giris denemesi: {KullaniciAdi}.", istek.KullaniciAdi);
            throw new HesapPasifException();
        }

        if (!_sifreHashleyici.Dogrula(istek.Sifre, personel.SifreHash))
        {
            personel.BasarisizGirisKaydet();
            await _dbContext.SaveChangesAsync(iptalToken);

            _logger.LogWarning(
                "Basarisiz giris denemesi (yanlis sifre): {KullaniciAdi}, ardisik hatali deneme: {Sayi}.",
                istek.KullaniciAdi, personel.BasarisizGirisSayisi);

            throw new GecersizGirisException();
        }

        personel.BasariliGirisKaydet();

        var yanit = await ErisimVeYenilemeJetonuUretVeKaydetAsync(personel, iptalToken);
        await _dbContext.SaveChangesAsync(iptalToken);

        _logger.LogInformation("Basarili giris: {KullaniciAdi} ({Rol}).", istek.KullaniciAdi, personel.Rol);

        return yanit;
    }

    public async Task<GirisYaniti> TokenYenileAsync(YenilemeIstegi istek, CancellationToken iptalToken = default)
    {
        ArgumentNullException.ThrowIfNull(istek);

        var gelenJetonHash = TokenHashOlustur(istek.YenilemeTokeni);
        var mevcutJeton = await _dbContext.YenilemeJetonlari
            .Include(j => j.Personel)
            .FirstOrDefaultAsync(j => j.TokenHash == gelenJetonHash, iptalToken);

        if (mevcutJeton is null || mevcutJeton.Personel is null || !mevcutJeton.GecerliMi())
        {
            _logger.LogWarning("Gecersiz veya suresi dolmus yenileme jetonu ile deneme yapildi.");
            throw new GecersizYenilemeJetonuException();
        }

        var personel = mevcutJeton.Personel;

        if (!personel.AktifMi || personel.KilitliMi())
        {
            _logger.LogWarning("Pasif/kilitli hesap icin yenileme jetonu kullanilmaya calisildi: {KullaniciAdi}.",
                personel.KullaniciAdi);
            throw new GecersizYenilemeJetonuException();
        }

        // Jeton rotasyonu: bu jeton bir daha kullanilamaz. Calinmis bir jetonun sinirsiz
        // sure kullanilabilmesini engeller; her yenilemede yeni bir jeton verilir.
        mevcutJeton.IptalEt();

        var yanit = await ErisimVeYenilemeJetonuUretVeKaydetAsync(personel, iptalToken);
        await _dbContext.SaveChangesAsync(iptalToken);

        return yanit;
    }

    private async Task<GirisYaniti> ErisimVeYenilemeJetonuUretVeKaydetAsync(Personel personel, CancellationToken iptalToken)
    {
        var (erisimTokeni, erisimSonKullanma) = _jwtTokenUretici.ErisimTokeniUret(personel);
        var yenilemeTokeniDegeri = _jwtTokenUretici.YenilemeTokeniUret();
        var yenilemeSonKullanma = _jwtTokenUretici.YenilemeTokeniSonKullanmaZamaniHesapla();

        var yeniYenilemeJetonu = new YenilemeJetonu(personel.Id, TokenHashOlustur(yenilemeTokeniDegeri), yenilemeSonKullanma);
        await _dbContext.YenilemeJetonlari.AddAsync(yeniYenilemeJetonu, iptalToken);

        return new GirisYaniti(
            erisimTokeni,
            erisimSonKullanma,
            yenilemeTokeniDegeri,
            yenilemeSonKullanma,
            personel.Id,
            personel.AdSoyad,
            personel.Rol.ToString());
    }

    /// <summary>
    /// Yenileme jetonu zaten kriptografik olarak guvenli, yuksek entropili rastgele bir deger
    /// oldugundan (bkz. JwtTokenUretici.YenilemeTokeniUret), sifrelerde oldugu gibi yavas/salted
    /// bir hash (BCrypt) yerine hizli SHA-256 yeterlidir; amac dusuk entropili girdilere karsi
    /// brute-force'u yavaslatmak degil, veritabani sizintisinda jetonun dogrudan kullanilamamasidir.
    /// </summary>
    private static string TokenHashOlustur(string duzMetinToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(duzMetinToken)));
}
