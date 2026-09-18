using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SahaGor.Domain.Enumlar;
using SahaGor.Domain.Ortak;
using SahaGor.Domain.Varliklar;
using SahaGor.Infrastructure.Atama;
using SahaGor.Infrastructure.Persistence;

namespace SahaGor.UnitTests.Atama;

public class AtamaMotoruTests
{
    private static SahaGorDbContext InBellekDbContextOlustur() =>
        new(new DbContextOptionsBuilder<SahaGorDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static AtamaMotoru MotorOlustur(SahaGorDbContext dbContext) =>
        new(dbContext, NullLogger<AtamaMotoru>.Instance);

    private static GorevKategorisi KategoriEkle(SahaGorDbContext dbContext)
    {
        var kategori = new GorevKategorisi("Kırık Kaldırım", slaYanitSuresiDakika: 60, slaCozumSuresiDakika: 480);
        dbContext.GorevKategorileri.Add(kategori);
        dbContext.SaveChanges();
        return kategori;
    }

    private static Personel AktifSahaPersoneliEkle(Birim birim, string kullaniciAdi) =>
        birim.PersonelEkle($"Personel {kullaniciAdi}", kullaniciAdi, "hash", "5550000000", PersonelRolu.SahaPersoneli);

    [Fact]
    public async Task OneriHesapla_Hicbir_Uygun_Ekip_Yoksa_OnerilenEkip_Null_Doner()
    {
        using var dbContext = InBellekDbContextOlustur();
        var kategori = KategoriEkle(dbContext);

        var gorev = new GorevTalebi("Kaldirim hasari", kategori, KonumFabrikasi.NoktaOlustur(41.0, 29.0),
            GorevOnceligi.Normal, GorevKaynagi.OperatorGirisi);
        dbContext.GorevTalepleri.Add(gorev);
        await dbContext.SaveChangesAsync();

        var motor = MotorOlustur(dbContext);
        var oneri = await motor.OneriHesaplaAsync(gorev.Id);

        Assert.Null(oneri.OnerilenEkip);
        Assert.Empty(oneri.TumAdaylar);
    }

    [Fact]
    public async Task OneriHesapla_Aktif_Uyesi_Olmayan_Ekibi_Aday_Olarak_Degerlendirmez()
    {
        using var dbContext = InBellekDbContextOlustur();
        var kategori = KategoriEkle(dbContext);
        var birim = new Birim(Guid.NewGuid(), "Fen Isleri Mudurlugu");
        var ekip = birim.EkipEkle("Bos Ekip"); // hic uyesi yok
        dbContext.Birimler.Add(birim);

        var gorev = new GorevTalebi("Kaldirim hasari", kategori, KonumFabrikasi.NoktaOlustur(41.0, 29.0),
            GorevOnceligi.Normal, GorevKaynagi.OperatorGirisi);
        dbContext.GorevTalepleri.Add(gorev);
        await dbContext.SaveChangesAsync();

        var motor = MotorOlustur(dbContext);
        var oneri = await motor.OneriHesaplaAsync(gorev.Id);

        Assert.Null(oneri.OnerilenEkip);
    }

    [Fact]
    public async Task OneriHesapla_Yakin_Musait_Yetkinlikli_Ekibi_Uzak_Ekibe_Tercih_Eder()
    {
        // Not: EF Core InMemory saglayicisi PostGIS'in kuresel (metre bazli) ST_Distance
        // hesabini degil, NetTopologySuite'in duzlemsel (derece bazli) Distance() metodunu
        // kullanir (bkz. GorevTalebiServisiTests'teki ayni notu). Bu test, mutlak metre
        // degerlerini degil SIRALAMAYI (yakin ekip > uzak ekip) dogrular; gercek metre
        // hassasiyeti Sprint 4'teki Testcontainers/PostGIS entegrasyon testinde dogrulanacaktir.
        using var dbContext = InBellekDbContextOlustur();
        var kategori = KategoriEkle(dbContext);
        var birim = new Birim(Guid.NewGuid(), "Fen Isleri Mudurlugu");

        var gorevKonumu = (Enlem: 41.0, Boylam: 29.0);

        // Yakin ekip: gorevle ayni konumda, musait, yetkinlik uyumlu, is yuku yok -> en yuksek skor beklenir.
        var yakinEkip = birim.EkipEkle("Yakin Ekip");
        yakinEkip.UzmanlikAlaniEkle(kategori);
        yakinEkip.KonumGuncelle(gorevKonumu.Enlem, gorevKonumu.Boylam);
        yakinEkip.UyeEkle(AktifSahaPersoneliEkle(birim, "yakin.personel"));

        // Uzak ekip: ayni sekilde musait ve yetkinlikli ama cok uzakta -> sadece mesafe skorunda kaybeder.
        var uzakEkip = birim.EkipEkle("Uzak Ekip");
        uzakEkip.UzmanlikAlaniEkle(kategori);
        uzakEkip.KonumGuncelle(10.0, 10.0);
        uzakEkip.UyeEkle(AktifSahaPersoneliEkle(birim, "uzak.personel"));

        dbContext.Birimler.Add(birim);

        var gorev = new GorevTalebi("Kaldirim hasari", kategori,
            KonumFabrikasi.NoktaOlustur(gorevKonumu.Enlem, gorevKonumu.Boylam),
            GorevOnceligi.Normal, GorevKaynagi.OperatorGirisi);
        dbContext.GorevTalepleri.Add(gorev);
        await dbContext.SaveChangesAsync();

        var motor = MotorOlustur(dbContext);
        var oneri = await motor.OneriHesaplaAsync(gorev.Id);

        Assert.NotNull(oneri.OnerilenEkip);
        Assert.Equal("Yakin Ekip", oneri.OnerilenEkip!.EkipAdi);
        Assert.Equal(1.0, oneri.OnerilenEkip.MesafeSkoru, precision: 2);
        Assert.Equal(2, oneri.TumAdaylar.Count);

        var uzakAday = oneri.TumAdaylar.Single(a => a.EkipAdi == "Uzak Ekip");
        Assert.True(oneri.OnerilenEkip.ToplamSkor > uzakAday.ToplamSkor);
    }

    [Fact]
    public async Task OneriHesapla_Yetkinligi_Olmayan_Ekip_Yetkinlik_Bileseninde_Sifir_Alir()
    {
        using var dbContext = InBellekDbContextOlustur();
        var kategori = KategoriEkle(dbContext);
        var birim = new Birim(Guid.NewGuid(), "Fen Isleri Mudurlugu");

        var ekip = birim.EkipEkle("Ekip");
        // Bilerek UzmanlikAlaniEkle cagrilmiyor: bu kategoride yetkinligi yok.
        ekip.UyeEkle(AktifSahaPersoneliEkle(birim, "personel1"));
        dbContext.Birimler.Add(birim);

        var gorev = new GorevTalebi("Kaldirim hasari", kategori, KonumFabrikasi.NoktaOlustur(41.0, 29.0),
            GorevOnceligi.Normal, GorevKaynagi.OperatorGirisi);
        dbContext.GorevTalepleri.Add(gorev);
        await dbContext.SaveChangesAsync();

        var motor = MotorOlustur(dbContext);
        var oneri = await motor.OneriHesaplaAsync(gorev.Id);

        Assert.NotNull(oneri.OnerilenEkip);
        Assert.Equal(0.0, oneri.OnerilenEkip!.YetkinlikSkoru);
    }
}
