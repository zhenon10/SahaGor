using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SahaGor.Application.Entegrasyonlar;
using SahaGor.Application.Entegrasyonlar.Dtolar;
using SahaGor.Application.Entegrasyonlar.Istisnalar;
using SahaGor.Application.Gorevler;
using SahaGor.Application.Gorevler.Dtolar;
using SahaGor.Domain.Enumlar;
using SahaGor.Infrastructure.Persistence;

namespace SahaGor.Infrastructure.Entegrasyonlar;

/// <summary>
/// Dis kaynakli basvurulari, mevcut GorevTalebiServisi.OlusturAsync akisi uzerinden
/// GorevTalebi'ne cevirir (SG-401). Ayni akisi yeniden kullanmak, otomatik bolgelendirme
/// (SG-115) ve atama motoru onerisinin (SG-310) 153 basvurulari icin de calismasini saglar.
/// </summary>
public sealed class DisKaynakBasvuruServisi : IDisKaynakBasvuruServisi
{
    private readonly SahaGorDbContext _dbContext;
    private readonly IGorevTalebiServisi _gorevTalebiServisi;
    private readonly ILogger<DisKaynakBasvuruServisi> _logger;

    public DisKaynakBasvuruServisi(SahaGorDbContext dbContext, IGorevTalebiServisi gorevTalebiServisi,
        ILogger<DisKaynakBasvuruServisi> logger)
    {
        _dbContext = dbContext;
        _gorevTalebiServisi = gorevTalebiServisi;
        _logger = logger;
    }

    public async Task<GorevTalebiDetayYaniti> Hat153BasvurusuIsleAsync(Hat153BasvuruIstegi istek,
        CancellationToken iptalToken = default)
    {
        // Idempotentlik (SG-423, OWASP A04/A08): 153 sistemi aginin zaman asimi nedeniyle ayni
        // basvuruyu tekrar gonderebilir, veya ayni imzali istek kotu niyetle tekrar oynatilabilir
        // (replay). Ayni referans numarasiyla daha once islenmis bir gorev varsa, YENISINI
        // OLUSTURMADAN mevcut olani doneriz. Veritabanindaki benzersiz index (bkz.
        // GorevTalebiConfiguration) ayni anda gelen iki istek arasindaki yarisma durumuna
        // (race condition) karsi son savunma hattidir.
        if (!string.IsNullOrWhiteSpace(istek.ReferansNo))
        {
            var mevcutGorev = await _dbContext.GorevTalepleri
                .FirstOrDefaultAsync(g => g.DisKaynakReferansNo == istek.ReferansNo, iptalToken);

            if (mevcutGorev is not null)
            {
                _logger.LogInformation(
                    "153 basvurusu ({ReferansNo}) daha once islenmis; mevcut gorev tekrar dondu.",
                    istek.ReferansNo);
                return await _gorevTalebiServisi.DetayGetirAsync(mevcutGorev.Id, iptalToken);
            }
        }

        var kategori = await _dbContext.GorevKategorileri
            .FirstOrDefaultAsync(k => k.Ad == istek.KategoriAdi && k.AktifMi, iptalToken)
            ?? throw new GorevKategorisiAdiylaBulunamadiException(istek.KategoriAdi);

        var olusturIstegi = new GorevTalebiOlusturIstegi(
            istek.Baslik,
            istek.Aciklama,
            kategori.Id,
            istek.Enlem,
            istek.Boylam,
            GorevOnceligi.Normal,
            GorevKaynagi.Hat153,
            istek.BildirenTelefonu,
            istek.ReferansNo);

        _logger.LogInformation("153 hattindan yeni basvuru alindi ve isleniyor: {ReferansNo}.", istek.ReferansNo);

        return await _gorevTalebiServisi.OlusturAsync(olusturIstegi, olusturanPersonelId: null, iptalToken);
    }
}
