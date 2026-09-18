using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SahaGor.Application.Birimler.Istisnalar;
using SahaGor.Application.Kimlik;
using SahaGor.Application.Personeller;
using SahaGor.Application.Personeller.Dtolar;
using SahaGor.Application.Personeller.Istisnalar;
using SahaGor.Domain.Varliklar;
using SahaGor.Infrastructure.Persistence;

namespace SahaGor.Infrastructure.Personeller;

public sealed class PersonelServisi : IPersonelServisi
{
    private readonly SahaGorDbContext _dbContext;
    private readonly ISifreHashleyici _sifreHashleyici;
    private readonly ILogger<PersonelServisi> _logger;

    public PersonelServisi(SahaGorDbContext dbContext, ISifreHashleyici sifreHashleyici, ILogger<PersonelServisi> logger)
    {
        _dbContext = dbContext;
        _sifreHashleyici = sifreHashleyici;
        _logger = logger;
    }

    public async Task<PersonelYaniti> OlusturAsync(PersonelOlusturIstegi istek, CancellationToken iptalToken = default)
    {
        ArgumentNullException.ThrowIfNull(istek);

        var birim = await _dbContext.Birimler.FirstOrDefaultAsync(b => b.Id == istek.BirimId, iptalToken)
            ?? throw new BirimBulunamadiException(istek.BirimId);

        var kullaniciAdiKullanimda = await _dbContext.Personeller
            .AnyAsync(p => p.KullaniciAdi == istek.KullaniciAdi, iptalToken);

        if (kullaniciAdiKullanimda)
        {
            throw new KullaniciAdiZatenKullanimdaException(istek.KullaniciAdi);
        }

        var sifreHash = _sifreHashleyici.Hashle(istek.Sifre);
        var personel = new Personel(birim.Id, istek.AdSoyad, istek.KullaniciAdi, sifreHash, istek.Telefon,
            istek.Rol, istek.Eposta);

        _dbContext.Personeller.Add(personel);
        await _dbContext.SaveChangesAsync(iptalToken);

        _logger.LogInformation("Yeni personel olusturuldu: {PersonelId} - {KullaniciAdi} ({Rol}).",
            personel.Id, personel.KullaniciAdi, personel.Rol);

        return Yanita(personel, birim.Ad, ekipAdi: null);
    }

    public async Task<IReadOnlyList<PersonelYaniti>> ListeleAsync(PersonelFiltre filtre, CancellationToken iptalToken = default)
    {
        ArgumentNullException.ThrowIfNull(filtre);

        var sorgu = _dbContext.Personeller.Include(p => p.Birim).Include(p => p.Ekip).AsQueryable();

        if (filtre.BirimId.HasValue)
        {
            sorgu = sorgu.Where(p => p.BirimId == filtre.BirimId.Value);
        }

        if (filtre.Rol.HasValue)
        {
            sorgu = sorgu.Where(p => p.Rol == filtre.Rol.Value);
        }

        if (filtre.AktifMi.HasValue)
        {
            sorgu = sorgu.Where(p => p.AktifMi == filtre.AktifMi.Value);
        }

        return await sorgu
            .OrderBy(p => p.AdSoyad)
            .Select(p => new PersonelYaniti(p.Id, p.BirimId, p.Birim!.Ad, p.EkipId, p.Ekip != null ? p.Ekip.Ad : null,
                p.AdSoyad, p.KullaniciAdi, p.Telefon, p.Eposta, p.Rol.ToString(), p.AktifMi, p.MusaitMi,
                p.KilitlenmeBitisZamaniUtc != null && p.KilitlenmeBitisZamaniUtc > DateTime.UtcNow))
            .ToListAsync(iptalToken);
    }

    public async Task<PersonelYaniti> DetayGetirAsync(Guid id, CancellationToken iptalToken = default)
    {
        var personel = await BulAsync(id, iptalToken);
        return Yanita(personel, personel.Birim!.Ad, personel.Ekip?.Ad);
    }

    public async Task<PersonelYaniti> GuncelleAsync(Guid id, PersonelGuncelleIstegi istek, CancellationToken iptalToken = default)
    {
        ArgumentNullException.ThrowIfNull(istek);

        var personel = await BulAsync(id, iptalToken);
        personel.TemelBilgileriGuncelle(istek.AdSoyad, istek.Telefon, istek.Eposta);
        await _dbContext.SaveChangesAsync(iptalToken);

        return Yanita(personel, personel.Birim!.Ad, personel.Ekip?.Ad);
    }

    public async Task DurumGuncelleAsync(Guid id, bool aktifMi, CancellationToken iptalToken = default)
    {
        var personel = await BulAsync(id, iptalToken);

        if (aktifMi)
        {
            personel.Aktiflestir();
        }
        else
        {
            personel.Pasiflestir();
        }

        await _dbContext.SaveChangesAsync(iptalToken);
    }

    public async Task SifreSifirlaAsync(Guid id, SifreSifirlaIstegi istek, CancellationToken iptalToken = default)
    {
        ArgumentNullException.ThrowIfNull(istek);

        var personel = await BulAsync(id, iptalToken);
        personel.SifreyiGuncelle(_sifreHashleyici.Hashle(istek.YeniSifre));
        await _dbContext.SaveChangesAsync(iptalToken);

        _logger.LogInformation("Personel sifresi idari olarak sifirlandi: {PersonelId}.", personel.Id);
    }

    private async Task<Personel> BulAsync(Guid id, CancellationToken iptalToken)
    {
        return await _dbContext.Personeller
            .Include(p => p.Birim)
            .Include(p => p.Ekip)
            .FirstOrDefaultAsync(p => p.Id == id, iptalToken)
            ?? throw new PersonelBulunamadiException(id);
    }

    private static PersonelYaniti Yanita(Personel personel, string birimAdi, string? ekipAdi) =>
        new(personel.Id, personel.BirimId, birimAdi, personel.EkipId, ekipAdi, personel.AdSoyad,
            personel.KullaniciAdi, personel.Telefon, personel.Eposta, personel.Rol.ToString(),
            personel.AktifMi, personel.MusaitMi, personel.KilitliMi());
}
