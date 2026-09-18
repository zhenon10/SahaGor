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
