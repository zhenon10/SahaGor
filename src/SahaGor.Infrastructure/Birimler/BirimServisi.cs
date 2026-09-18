using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SahaGor.Application.Birimler;
using SahaGor.Application.Birimler.Dtolar;
using SahaGor.Application.Birimler.Istisnalar;
using SahaGor.Application.Kurumlar.Istisnalar;
using SahaGor.Domain.Varliklar;
using SahaGor.Infrastructure.Persistence;

namespace SahaGor.Infrastructure.Birimler;

public sealed class BirimServisi : IBirimServisi
{
    private readonly SahaGorDbContext _dbContext;
    private readonly ILogger<BirimServisi> _logger;

    public BirimServisi(SahaGorDbContext dbContext, ILogger<BirimServisi> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<BirimYaniti> OlusturAsync(BirimOlusturIstegi istek, CancellationToken iptalToken = default)
    {
        ArgumentNullException.ThrowIfNull(istek);

        var kurum = await _dbContext.Kurumlar.FirstOrDefaultAsync(k => k.Id == istek.KurumId, iptalToken)
            ?? throw new KurumBulunamadiException(istek.KurumId);

        var birim = new Birim(kurum.Id, istek.Ad, istek.Aciklama);
        _dbContext.Birimler.Add(birim);
        await _dbContext.SaveChangesAsync(iptalToken);

        _logger.LogInformation("Yeni birim olusturuldu: {BirimId} - {Ad} ({KurumAdi}).", birim.Id, birim.Ad, kurum.Ad);

        return Yanita(birim, kurum.Ad, personelSayisi: 0, ekipSayisi: 0);
    }

    public async Task<IReadOnlyList<BirimYaniti>> ListeleAsync(Guid? kurumId, CancellationToken iptalToken = default)
    {
        var sorgu = _dbContext.Birimler.Include(b => b.Kurum).AsQueryable();

        if (kurumId.HasValue)
        {
            sorgu = sorgu.Where(b => b.KurumId == kurumId.Value);
        }

        return await sorgu
            .OrderBy(b => b.Ad)
            .Select(b => new BirimYaniti(b.Id, b.KurumId, b.Kurum!.Ad, b.Ad, b.Aciklama, b.AktifMi,
                b.Personeller.Count, b.Ekipler.Count))
            .ToListAsync(iptalToken);
    }

    public async Task<BirimYaniti> DetayGetirAsync(Guid id, CancellationToken iptalToken = default)
    {
        var birim = await BulAsync(id, iptalToken);
        return Yanita(birim, birim.Kurum!.Ad, birim.Personeller.Count, birim.Ekipler.Count);
    }

    public async Task<BirimYaniti> GuncelleAsync(Guid id, BirimGuncelleIstegi istek, CancellationToken iptalToken = default)
    {
        ArgumentNullException.ThrowIfNull(istek);

        var birim = await BulAsync(id, iptalToken);
        birim.BilgileriGuncelle(istek.Ad, istek.Aciklama);
        await _dbContext.SaveChangesAsync(iptalToken);

        return Yanita(birim, birim.Kurum!.Ad, birim.Personeller.Count, birim.Ekipler.Count);
    }

    public async Task DurumGuncelleAsync(Guid id, bool aktifMi, CancellationToken iptalToken = default)
    {
        var birim = await BulAsync(id, iptalToken);

        if (aktifMi)
        {
            birim.Aktiflestir();
        }
        else
        {
            birim.Pasiflestir();
        }

        await _dbContext.SaveChangesAsync(iptalToken);
    }

    private async Task<Birim> BulAsync(Guid id, CancellationToken iptalToken)
    {
        return await _dbContext.Birimler
            .Include(b => b.Kurum)
            .Include(b => b.Personeller)
            .Include(b => b.Ekipler)
            .FirstOrDefaultAsync(b => b.Id == id, iptalToken)
            ?? throw new BirimBulunamadiException(id);
    }

    private static BirimYaniti Yanita(Birim birim, string kurumAdi, int personelSayisi, int ekipSayisi) =>
        new(birim.Id, birim.KurumId, kurumAdi, birim.Ad, birim.Aciklama, birim.AktifMi, personelSayisi, ekipSayisi);
}
