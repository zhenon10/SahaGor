using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SahaGor.Application.Kurumlar;
using SahaGor.Application.Kurumlar.Dtolar;
using SahaGor.Application.Kurumlar.Istisnalar;
using SahaGor.Domain.Varliklar;
using SahaGor.Infrastructure.Persistence;

namespace SahaGor.Infrastructure.Kurumlar;

public sealed class KurumServisi : IKurumServisi
{
    private readonly SahaGorDbContext _dbContext;
    private readonly ILogger<KurumServisi> _logger;

    public KurumServisi(SahaGorDbContext dbContext, ILogger<KurumServisi> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<KurumYaniti> OlusturAsync(KurumOlusturIstegi istek, CancellationToken iptalToken = default)
    {
        ArgumentNullException.ThrowIfNull(istek);

        var kurum = new Kurum(istek.Ad, istek.Adres, istek.IletisimTelefonu);
        _dbContext.Kurumlar.Add(kurum);
        await _dbContext.SaveChangesAsync(iptalToken);

        _logger.LogInformation("Yeni kurum olusturuldu: {KurumId} - {Ad}.", kurum.Id, kurum.Ad);

        return Yanita(kurum, birimSayisi: 0);
    }

    public async Task<IReadOnlyList<KurumYaniti>> ListeleAsync(CancellationToken iptalToken = default)
    {
        return await _dbContext.Kurumlar
            .OrderBy(k => k.Ad)
            .Select(k => new KurumYaniti(k.Id, k.Ad, k.Adres, k.IletisimTelefonu, k.AktifMi, k.Birimler.Count))
            .ToListAsync(iptalToken);
    }

    public async Task<KurumYaniti> DetayGetirAsync(Guid id, CancellationToken iptalToken = default)
    {
        var kurum = await BulAsync(id, iptalToken);
        return Yanita(kurum, kurum.Birimler.Count);
    }

    public async Task<KurumYaniti> GuncelleAsync(Guid id, KurumGuncelleIstegi istek, CancellationToken iptalToken = default)
    {
        ArgumentNullException.ThrowIfNull(istek);

        var kurum = await BulAsync(id, iptalToken);
        kurum.BilgileriGuncelle(istek.Ad, istek.Adres, istek.IletisimTelefonu);
        await _dbContext.SaveChangesAsync(iptalToken);

        return Yanita(kurum, kurum.Birimler.Count);
    }

    public async Task DurumGuncelleAsync(Guid id, bool aktifMi, CancellationToken iptalToken = default)
    {
        var kurum = await BulAsync(id, iptalToken);

        if (aktifMi)
        {
            kurum.Aktiflestir();
        }
        else
        {
            kurum.Pasiflestir();
        }

        await _dbContext.SaveChangesAsync(iptalToken);
    }

    private async Task<Kurum> BulAsync(Guid id, CancellationToken iptalToken)
    {
        return await _dbContext.Kurumlar
            .Include(k => k.Birimler)
            .FirstOrDefaultAsync(k => k.Id == id, iptalToken)
            ?? throw new KurumBulunamadiException(id);
    }

    private static KurumYaniti Yanita(Kurum kurum, int birimSayisi) =>
        new(kurum.Id, kurum.Ad, kurum.Adres, kurum.IletisimTelefonu, kurum.AktifMi, birimSayisi);
}
