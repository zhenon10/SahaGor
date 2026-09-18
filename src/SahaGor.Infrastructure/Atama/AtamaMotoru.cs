using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SahaGor.Application.Atama;
using SahaGor.Application.Atama.Dtolar;
using SahaGor.Application.Gorevler.Istisnalar;
using SahaGor.Domain.Enumlar;
using SahaGor.Domain.Varliklar;
using SahaGor.Infrastructure.Persistence;

namespace SahaGor.Infrastructure.Atama;

/// <summary>
/// SG-310 agirlikli skorlama algoritmasinin EF Core/PostGIS implementasyonu.
///
/// Skor bilesenleri:
///  - Mesafe (%40): ekip <-> gorev arasi PostGIS ST_Distance (metre). 0m = tam puan,
///    "MesafeUstSiniriMetre" ve otesi = 0 puan. Konumu bilinmeyen ekip 0 puan alir
///    (elenmez, sadece bu bilesende dezavantajli olur).
///  - Musaitlik (%30): Ekip.MusaitMi() - aktif VE musait en az bir uyesi var mi.
///  - Yetkinlik (%20): Ekip.UzmanMi(kategori) - gorevin kategorisi ekibin kayitli
///    uzmanlik alanlarindan biri mi (katı eslesme; genelleme yapilmaz).
///  - Is yuku (%10): ekibin uzerindeki acik (kapanmamis) gorev sayisi arttikca duser.
/// </summary>
public sealed class AtamaMotoru : IAtamaMotoru
{
    private const double MesafeAgirligi = 0.40;
    private const double MusaitlikAgirligi = 0.30;
    private const double YetkinlikAgirligi = 0.20;
    private const double IsYukuAgirligi = 0.10;

    /// <summary>Bu mesafenin (metre) otesindeki ekipler mesafe bileseninde 0 puan alir.</summary>
    private const double MesafeUstSiniriMetre = 10_000;

    /// <summary>Bu sayida veya daha fazla aktif gorevi olan ekip is yuku bileseninde 0 puan alir.</summary>
    private const int IsYukuEsikGorevSayisi = 5;

    private static readonly GorevDurumu[] KapanmisDurumlar =
    {
        GorevDurumu.Tamamlandi, GorevDurumu.Dogrulandi, GorevDurumu.Iptal,
    };

    private readonly SahaGorDbContext _dbContext;
    private readonly ILogger<AtamaMotoru> _logger;

    public AtamaMotoru(SahaGorDbContext dbContext, ILogger<AtamaMotoru> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<AtamaOnerisiYaniti> OneriHesaplaAsync(Guid gorevId, CancellationToken iptalToken = default)
    {
        var gorev = await _dbContext.GorevTalepleri.FirstOrDefaultAsync(g => g.Id == gorevId, iptalToken)
            ?? throw new GorevTalebiBulunamadiException(gorevId);

        var gorevKonumu = gorev.Konum;

        var adayEkipler = await _dbContext.Ekipler
            .Include(e => e.Uyeler)
            .Include(e => e.UzmanlikAlanlari)
            .Where(e => e.AktifMi)
            .ToListAsync(iptalToken);

        // "Uygun" ekip: aktif VE en az bir aktif uyesi olan ekip. Bu, katilim disi
        // birakilan tek sert filtredir; musaitlik/yetkinlik/is yuku birer AGIRLIKLI
        // puan bilesenidir, eleme kriteri degildir (dusuk puanli bir ekip yine de
        // Amir tarafindan manuel secilebilmelidir - SG-311).
        var uygunEkipler = adayEkipler.Where(e => e.Uyeler.Any(u => u.AktifMi)).ToList();

        if (uygunEkipler.Count == 0)
        {
            _logger.LogWarning(
                "Gorev {GorevId} icin atama onerisi hesaplanamadi: aktif uyesi olan hicbir aktif ekip yok.", gorevId);
            return new AtamaOnerisiYaniti(gorevId, null, Array.Empty<AtamaAdayiYaniti>());
        }

        var ekipIdleri = uygunEkipler.Select(e => e.Id).ToList();

        // Mesafe: ST_Distance'in dogru (kuresel, metre cinsinden) sonuc vermesi icin
        // hesaplama VERITABANI TARAFINDA (LINQ-to-Entities) yapilir; entity'ler bellege
        // alindiktan sonra NetTopologySuite'in duzlemsel Distance() metodu KULLANILMAZ
        // (aksi halde derece biriminde anlamsiz bir deger elde edilirdi).
        var mesafelerMetre = await _dbContext.Ekipler
            .Where(e => ekipIdleri.Contains(e.Id))
            .Select(e => new { e.Id, Mesafe = e.GuncelKonum != null ? (double?)e.GuncelKonum.Distance(gorevKonumu) : null })
            .ToDictionaryAsync(x => x.Id, x => x.Mesafe, iptalToken);

        var aktifGorevSayilari = await _dbContext.GorevTalepleri
            .Where(g => g.AtananEkipId != null && ekipIdleri.Contains(g.AtananEkipId!.Value) &&
                !KapanmisDurumlar.Contains(g.Durum))
            .GroupBy(g => g.AtananEkipId!.Value)
            .Select(grp => new { EkipId = grp.Key, Sayi = grp.Count() })
            .ToDictionaryAsync(x => x.EkipId, x => x.Sayi, iptalToken);

        var skorluAdaylar = uygunEkipler
            .Select(ekip => SkorHesapla(ekip, gorev.KategoriId, mesafelerMetre[ekip.Id],
                aktifGorevSayilari.GetValueOrDefault(ekip.Id)))
            .OrderByDescending(aday => aday.ToplamSkor)
            .ToList();

        var onerilenEkip = skorluAdaylar[0];

        _logger.LogInformation(
            "Gorev {GorevId} icin atama onerisi: '{EkipAdi}' (toplam skor: {ToplamSkor:F2} | mesafe: {MesafeSkoru:F2}, " +
            "musaitlik: {MusaitlikSkoru:F2}, yetkinlik: {YetkinlikSkoru:F2}, is yuku: {IsYukuSkoru:F2}). " +
            "Degerlendirilen aday sayisi: {AdaySayisi}.",
            gorevId, onerilenEkip.EkipAdi, onerilenEkip.ToplamSkor, onerilenEkip.MesafeSkoru,
            onerilenEkip.MusaitlikSkoru, onerilenEkip.YetkinlikSkoru, onerilenEkip.IsYukuSkoru, skorluAdaylar.Count);

        return new AtamaOnerisiYaniti(gorevId, onerilenEkip, skorluAdaylar);
    }

    private static AtamaAdayiYaniti SkorHesapla(Ekip ekip, Guid gorevKategoriId,
        double? mesafeMetre, int aktifGorevSayisi)
    {
        var mesafeSkoru = mesafeMetre.HasValue
            ? Math.Clamp(1 - mesafeMetre.Value / MesafeUstSiniriMetre, 0, 1)
            : 0;

        var musaitlikSkoru = ekip.MusaitMi() ? 1.0 : 0.0;
        var yetkinlikSkoru = ekip.UzmanMi(gorevKategoriId) ? 1.0 : 0.0;
        var isYukuSkoru = Math.Clamp(1 - (double)aktifGorevSayisi / IsYukuEsikGorevSayisi, 0, 1);

        var toplamSkor =
            mesafeSkoru * MesafeAgirligi +
            musaitlikSkoru * MusaitlikAgirligi +
            yetkinlikSkoru * YetkinlikAgirligi +
            isYukuSkoru * IsYukuAgirligi;

        return new AtamaAdayiYaniti(
            ekip.Id,
            ekip.Ad,
            Math.Round(toplamSkor, 4),
            Math.Round(mesafeSkoru, 4),
            musaitlikSkoru,
            yetkinlikSkoru,
            Math.Round(isYukuSkoru, 4),
            mesafeMetre.HasValue ? Math.Round(mesafeMetre.Value, 1) : null,
            aktifGorevSayisi);
    }
}
