using Microsoft.Extensions.Logging.Abstractions;
using SahaGor.Application.Bildirimler;
using SahaGor.Application.Gorevler;
using SahaGor.Application.Gorevler.Dtolar;
using SahaGor.Domain.Enumlar;
using SahaGor.Domain.Ortak;
using SahaGor.Domain.Varliklar;
using SahaGor.Infrastructure.Atama;
using SahaGor.Infrastructure.Gorevler;
using SahaGor.Infrastructure.Sms;

namespace SahaGor.IntegrationTests.Gorevler;

/// <summary>
/// SG-420: GorevTalebiServisi'nin cografi sorgularini GERCEK bir PostgreSQL/PostGIS
/// konteynerine karsi calistirir. Birim testlerindeki (SahaGor.UnitTests) InMemory saglayicili
/// esdegerlerinin aksine, buradaki sonuclar gercek ST_DWithin/ST_Distance/ST_Contains SQL
/// fonksiyonlarindan gecer; amac, sadece uygulama mantigini degil, LINQ->SQL cevirisinin ve
/// kolon tiplerinin (geography) production'da da dogru calistigini dogrulamaktir.
/// </summary>
[Collection(PostGisTestCollection.Adi)]
public class GorevTalebiServisiPostGisTests
{
    /// <summary>Testlerde gercek SignalR altyapisina ihtiyac olmadigindan kullanilan sahte (no-op) yayinlayici.</summary>
    private sealed class SahteGorevBildirimYayinlayici : IGorevBildirimYayinlayici
    {
        public Task YayinlaAsync(GorevBildirimi bildirim, CancellationToken iptalToken = default) => Task.CompletedTask;
    }

    /// <summary>Testlerde gercek diske yazmadan, dosya icerigini bellekte tutan sahte depolama.</summary>
    private sealed class SahteFotografDepolamaServisi : IFotografDepolamaServisi
    {
        public async Task<string> KaydetAsync(Stream icerik, string dosyaUzantisi, CancellationToken iptalToken = default)
        {
            using var bellekAkisi = new MemoryStream();
            await icerik.CopyToAsync(bellekAkisi, iptalToken);
            return $"sahte-{Guid.NewGuid()}{dosyaUzantisi}";
        }
    }

    private readonly PostGisTestFixture _fixture;

    public GorevTalebiServisiPostGisTests(PostGisTestFixture fixture)
    {
        _fixture = fixture;
    }

    private static GorevTalebiServisi ServisOlustur(SahaGor.Infrastructure.Persistence.SahaGorDbContext dbContext) =>
        new(dbContext, new SahteGorevBildirimYayinlayici(), new SahteFotografDepolamaServisi(),
            new AtamaMotoru(dbContext, NullLogger<AtamaMotoru>.Instance),
            new LoglayanSmsGonderimServisi(NullLogger<LoglayanSmsGonderimServisi>.Instance),
            NullLogger<GorevTalebiServisi>.Instance);

    /// <summary>
    /// Verilen merkez noktadan, belirtilen boylam farkinin (WGS84, bu enlemde) yaklasik kac
    /// metreye denk geldigini hesaplar. Kisa (birkac yuz metrelik) mesafelerde duzlemsel
    /// yaklasim, PostGIS'in gercek kuresel ST_Distance sonucuna santimetre mertebesinde
    /// yakin kalir; bu yuzden test toleranslari (+-50m) rahatlikla karsilanir.
    /// </summary>
    private static double BoylamFarkindanMetreyeYaklasikCevir(double enlemDerece, double metre)
    {
        const double DunyaYaricapiMetre = 6_371_000;
        var enlemRadyan = enlemDerece * Math.PI / 180;
        var metreBasinaDerece = 1 / (DunyaYaricapiMetre * Math.Cos(enlemRadyan) * Math.PI / 180);
        return metre * metreBasinaDerece;
    }

    [Fact]
    public async Task Yakinimdaki_Gercek_PostGIS_Kuresel_Mesafeye_Gore_Yaricap_Disindakini_Disarida_Birakir()
    {
        await using var dbContext = _fixture.DbContextOlustur();

        var kategori = new GorevKategorisi($"Test Kategori {Guid.NewGuid():N}", slaYanitSuresiDakika: 30,
            slaCozumSuresiDakika: 480);
        dbContext.GorevKategorileri.Add(kategori);
        await dbContext.SaveChangesAsync();

        const double merkezEnlem = 41.0;
        const double merkezBoylam = 29.0;

        // ~400m ve ~600m uzaklikta iki gorev; 500m yaricapli sorgu SADECE ilkini donmeli.
        var yakinBoylamFarki = BoylamFarkindanMetreyeYaklasikCevir(merkezEnlem, 400);
        var uzakBoylamFarki = BoylamFarkindanMetreyeYaklasikCevir(merkezEnlem, 600);

        var yakinGorev = new GorevTalebi("400m yakin gorev", kategori,
            KonumFabrikasi.NoktaOlustur(merkezEnlem, merkezBoylam + yakinBoylamFarki),
            GorevOnceligi.Normal, GorevKaynagi.OperatorGirisi);
        var uzakGorev = new GorevTalebi("600m uzak gorev", kategori,
            KonumFabrikasi.NoktaOlustur(merkezEnlem, merkezBoylam + uzakBoylamFarki),
            GorevOnceligi.Normal, GorevKaynagi.OperatorGirisi);

        dbContext.GorevTalepleri.AddRange(yakinGorev, uzakGorev);
        await dbContext.SaveChangesAsync();

        var servis = ServisOlustur(dbContext);
        var sonuc = await servis.YakinimdakiGetirAsync(merkezEnlem, merkezBoylam, yaricapMetre: 500,
            maksimumSonucSayisi: 10);

        var kayit = Assert.Single(sonuc);
        Assert.Equal("400m yakin gorev", kayit.Baslik);
        // Gercek ST_Distance (kuresel/metre) sonucu, hedeflenen ~400m'ye yakin olmali; InMemory
        // saglayicisinda bu deger derece-bazli oldugundan anlamsiz kucuk bir sayi cikardi.
        Assert.InRange(kayit.MesafeMetre, 350, 450);
    }

    [Fact]
    public async Task Olustur_Bolge_Sinirlari_Icindeki_Konum_Icin_Bolgeyi_Otomatik_Atar()
    {
        await using var dbContext = _fixture.DbContextOlustur();

        var kategori = new GorevKategorisi($"Test Kategori {Guid.NewGuid():N}", slaYanitSuresiDakika: 30,
            slaCozumSuresiDakika: 480);
        dbContext.GorevKategorileri.Add(kategori);

        var kurum = new Kurum($"Test Kurum {Guid.NewGuid():N}");
        dbContext.Kurumlar.Add(kurum);

        // Merkezi (41.0, 29.0) olan, yaklasik 0.02 derecelik (~2km) kare bir bolge siniri.
        var bolgePoligonu = KareBolgeOlustur(merkezEnlem: 41.0, merkezBoylam: 29.0, kenarUzunluguDerece: 0.02);
        var bolge = new Bolge(kurum.Id, $"Test Bolge {Guid.NewGuid():N}", bolgePoligonu);
        dbContext.Bolgeler.Add(bolge);
        await dbContext.SaveChangesAsync();

        var servis = ServisOlustur(dbContext);
        var istek = new GorevTalebiOlusturIstegi("Bolge ici gorev", null, kategori.Id, 41.0, 29.0,
            GorevOnceligi.Normal, GorevKaynagi.OperatorGirisi, null, null);

        var sonuc = await servis.OlusturAsync(istek, olusturanPersonelId: null);

        // Bu assert, SinirPolygonu.Contains(konum) LINQ ifadesinin (Bolge.NoktayiKapsiyorMu /
        // GorevTalebiServisi.OlusturAsync) "geography" kolonu uzerinde GERCEK PostGIS'e karsi
        // calisip calismadigini dogrular; InMemory saglayicisi bu SQL cevirisini hic test etmez.
        Assert.Equal(bolge.Ad, sonuc.BolgeAdi);
    }

    private static NetTopologySuite.Geometries.Polygon KareBolgeOlustur(double merkezEnlem, double merkezBoylam,
        double kenarUzunluguDerece)
    {
        var fabrika = NetTopologySuite.NtsGeometryServices.Instance.CreateGeometryFactory(KonumFabrikasi.WGS84SridNumarasi);
        var yari = kenarUzunluguDerece / 2;

        return fabrika.CreatePolygon(new[]
        {
            new NetTopologySuite.Geometries.Coordinate(merkezBoylam - yari, merkezEnlem - yari),
            new NetTopologySuite.Geometries.Coordinate(merkezBoylam + yari, merkezEnlem - yari),
            new NetTopologySuite.Geometries.Coordinate(merkezBoylam + yari, merkezEnlem + yari),
            new NetTopologySuite.Geometries.Coordinate(merkezBoylam - yari, merkezEnlem + yari),
            new NetTopologySuite.Geometries.Coordinate(merkezBoylam - yari, merkezEnlem - yari),
        });
    }
}
