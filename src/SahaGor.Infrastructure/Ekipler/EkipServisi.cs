using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SahaGor.Application.Bildirimler;
using SahaGor.Application.Birimler.Istisnalar;
using SahaGor.Application.Ekipler;
using SahaGor.Application.Ekipler.Dtolar;
using SahaGor.Application.Ekipler.Istisnalar;
using SahaGor.Application.Gorevler.Istisnalar;
using SahaGor.Application.Personeller.Istisnalar;
using SahaGor.Domain.Varliklar;
using SahaGor.Infrastructure.Persistence;

namespace SahaGor.Infrastructure.Ekipler;

public sealed class EkipServisi : IEkipServisi
{
    private readonly SahaGorDbContext _dbContext;
    private readonly IEkipBildirimYayinlayici _bildirimYayinlayici;
    private readonly ILogger<EkipServisi> _logger;

    public EkipServisi(SahaGorDbContext dbContext, IEkipBildirimYayinlayici bildirimYayinlayici, ILogger<EkipServisi> logger)
    {
        _dbContext = dbContext;
        _bildirimYayinlayici = bildirimYayinlayici;
        _logger = logger;
    }

    public async Task<EkipYaniti> OlusturAsync(EkipOlusturIstegi istek, CancellationToken iptalToken = default)
    {
        ArgumentNullException.ThrowIfNull(istek);

        var birim = await _dbContext.Birimler.FirstOrDefaultAsync(b => b.Id == istek.BirimId, iptalToken)
            ?? throw new BirimBulunamadiException(istek.BirimId);

        var ekip = new Ekip(birim.Id, istek.Ad);
        _dbContext.Ekipler.Add(ekip);
        await _dbContext.SaveChangesAsync(iptalToken);

        _logger.LogInformation("Yeni ekip olusturuldu: {EkipId} - {Ad} ({BirimAdi}).", ekip.Id, ekip.Ad, birim.Ad);

        return Yanita(ekip, birim.Ad);
    }

    public async Task<IReadOnlyList<EkipYaniti>> ListeleAsync(Guid? birimId, CancellationToken iptalToken = default)
    {
        var sorgu = _dbContext.Ekipler
            .Include(e => e.Birim)
            .Include(e => e.Uyeler)
            .Include(e => e.UzmanlikAlanlari)
            .AsQueryable();

        if (birimId.HasValue)
        {
            sorgu = sorgu.Where(e => e.BirimId == birimId.Value);
        }

        var ekipler = await sorgu.OrderBy(e => e.Ad).ToListAsync(iptalToken);
        return ekipler.Select(e => Yanita(e, e.Birim!.Ad)).ToList();
    }

    public async Task<EkipYaniti> DetayGetirAsync(Guid id, CancellationToken iptalToken = default)
    {
        var ekip = await BulAsync(id, iptalToken);
        return Yanita(ekip, ekip.Birim!.Ad);
    }

    public async Task<EkipYaniti> GuncelleAsync(Guid id, EkipGuncelleIstegi istek, CancellationToken iptalToken = default)
    {
        ArgumentNullException.ThrowIfNull(istek);

        var ekip = await BulAsync(id, iptalToken);
        ekip.AdiGuncelle(istek.Ad);
        await _dbContext.SaveChangesAsync(iptalToken);

        return Yanita(ekip, ekip.Birim!.Ad);
    }

    public async Task DurumGuncelleAsync(Guid id, bool aktifMi, CancellationToken iptalToken = default)
    {
        var ekip = await BulAsync(id, iptalToken);

        if (aktifMi)
        {
            ekip.Aktiflestir();
        }
        else
        {
            ekip.Pasiflestir();
        }

        await _dbContext.SaveChangesAsync(iptalToken);
    }

    public async Task<EkipYaniti> UyeEkleAsync(Guid ekipId, Guid personelId, CancellationToken iptalToken = default)
    {
        var ekip = await BulAsync(ekipId, iptalToken);

        var personel = await _dbContext.Personeller.FirstOrDefaultAsync(p => p.Id == personelId, iptalToken)
            ?? throw new PersonelBulunamadiException(personelId);

        ekip.UyeEkle(personel);
        await _dbContext.SaveChangesAsync(iptalToken);

        return Yanita(ekip, ekip.Birim!.Ad);
    }

    public async Task<EkipYaniti> UyeCikarAsync(Guid ekipId, Guid personelId, CancellationToken iptalToken = default)
    {
        var ekip = await BulAsync(ekipId, iptalToken);

        var personel = await _dbContext.Personeller.FirstOrDefaultAsync(p => p.Id == personelId, iptalToken)
            ?? throw new PersonelBulunamadiException(personelId);

        ekip.UyeCikar(personel);
        await _dbContext.SaveChangesAsync(iptalToken);

        return Yanita(ekip, ekip.Birim!.Ad);
    }

    public async Task<EkipYaniti> UzmanlikAlaniEkleAsync(Guid ekipId, Guid gorevKategorisiId, CancellationToken iptalToken = default)
    {
        var ekip = await BulAsync(ekipId, iptalToken);

        var kategori = await _dbContext.GorevKategorileri.FirstOrDefaultAsync(k => k.Id == gorevKategorisiId, iptalToken)
            ?? throw new GorevKategorisiBulunamadiException(gorevKategorisiId);

        ekip.UzmanlikAlaniEkle(kategori);
        await _dbContext.SaveChangesAsync(iptalToken);

        return Yanita(ekip, ekip.Birim!.Ad);
    }

    public async Task<EkipYaniti> UzmanlikAlaniCikarAsync(Guid ekipId, Guid gorevKategorisiId, CancellationToken iptalToken = default)
    {
        var ekip = await BulAsync(ekipId, iptalToken);

        ekip.UzmanlikAlaniCikar(gorevKategorisiId);
        await _dbContext.SaveChangesAsync(iptalToken);

        return Yanita(ekip, ekip.Birim!.Ad);
    }

    public async Task<EkipYaniti> KonumGuncelleAsync(Guid ekipId, Guid personelId, double enlem, double boylam,
        CancellationToken iptalToken = default)
    {
        var ekip = await BulAsync(ekipId, iptalToken);

        if (!ekip.Uyeler.Any(u => u.Id == personelId))
        {
            throw new EkipErisimYetkisiYokException(ekipId);
        }

        ekip.KonumGuncelle(enlem, boylam);
        await _dbContext.SaveChangesAsync(iptalToken);

        await _bildirimYayinlayici.YayinlaAsync(
            new EkipKonumBildirimi(ekip.Id, ekip.Ad, enlem, boylam, DateTime.UtcNow), iptalToken);

        return Yanita(ekip, ekip.Birim!.Ad);
    }

    private async Task<Ekip> BulAsync(Guid id, CancellationToken iptalToken)
    {
        return await _dbContext.Ekipler
            .Include(e => e.Birim)
            .Include(e => e.Uyeler)
            .Include(e => e.UzmanlikAlanlari)
            .FirstOrDefaultAsync(e => e.Id == id, iptalToken)
            ?? throw new EkipBulunamadiException(id);
    }

    private static EkipYaniti Yanita(Ekip ekip, string birimAdi) => new(
        ekip.Id,
        ekip.BirimId,
        birimAdi,
        ekip.Ad,
        ekip.AktifMi,
        ekip.GuncelKonum?.Y,
        ekip.GuncelKonum?.X,
        ekip.KonumGuncellenmeZamaniUtc,
        ekip.Uyeler.Select(u => new EkipUyesiYaniti(u.Id, u.AdSoyad, u.AktifMi, u.MusaitMi)).ToList(),
        ekip.UzmanlikAlanlari.Select(k => new EkipUzmanlikAlaniYaniti(k.Id, k.Ad)).ToList());
}
