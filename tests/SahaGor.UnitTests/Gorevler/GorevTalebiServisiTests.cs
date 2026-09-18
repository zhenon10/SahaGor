using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using SahaGor.Application.Bildirimler;
using SahaGor.Application.Gorevler;
using SahaGor.Application.Gorevler.Dtolar;
using SahaGor.Application.Gorevler.Istisnalar;
using SahaGor.Domain.Enumlar;
using SahaGor.Domain.Ortak;
using SahaGor.Domain.Varliklar;
using SahaGor.Infrastructure.Atama;
using SahaGor.Infrastructure.Gorevler;
using SahaGor.Infrastructure.Persistence;

namespace SahaGor.UnitTests.Gorevler;

public class GorevTalebiServisiTests
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

    /// <summary>Testlerde gercek SMS gonderimi yapmayan, sadece cagrildigini kaydeden sahte servis.</summary>
    private sealed class SahteSmsGonderimServisi : ISmsGonderimServisi
    {
        public List<(string TelefonNumarasi, string Mesaj)> GonderilenSmsler { get; } = new();

        public Task GonderAsync(string telefonNumarasi, string mesaj, CancellationToken iptalToken = default)
        {
            GonderilenSmsler.Add((telefonNumarasi, mesaj));
            return Task.CompletedTask;
        }
    }

    private static SahaGorDbContext InBellekDbContextOlustur() =>
        new(new DbContextOptionsBuilder<SahaGorDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static GorevTalebiServisi ServisOlustur(SahaGorDbContext dbContext, ISmsGonderimServisi? smsGonderimServisi = null) =>
        new(dbContext, new SahteGorevBildirimYayinlayici(), new SahteFotografDepolamaServisi(),
            new AtamaMotoru(dbContext, NullLogger<AtamaMotoru>.Instance),
            smsGonderimServisi ?? new SahteSmsGonderimServisi(),
            NullLogger<GorevTalebiServisi>.Instance);

    private static Polygon KareBolgeOlustur(double merkezEnlem, double merkezBoylam, double kenarUzunlugu)
    {
        var fabrika = NtsGeometryServices.Instance.CreateGeometryFactory(KonumFabrikasi.WGS84SridNumarasi);
        var yari = kenarUzunlugu / 2;

        return fabrika.CreatePolygon(new[]
        {
            new Coordinate(merkezBoylam - yari, merkezEnlem - yari),
            new Coordinate(merkezBoylam + yari, merkezEnlem - yari),
            new Coordinate(merkezBoylam + yari, merkezEnlem + yari),
            new Coordinate(merkezBoylam - yari, merkezEnlem + yari),
            new Coordinate(merkezBoylam - yari, merkezEnlem - yari),
        });
    }

    private static GorevKategorisi OrnekKategoriEkle(SahaGorDbContext dbContext, bool aktif = true)
    {
        var kategori = new GorevKategorisi("Kırık Kaldırım", slaYanitSuresiDakika: 60, slaCozumSuresiDakika: 480);
        if (!aktif)
        {
            kategori.Pasiflestir();
        }

        dbContext.GorevKategorileri.Add(kategori);
        dbContext.SaveChanges();
        return kategori;
    }

    [Fact]
    public async Task Olustur_Konum_Bolge_Sinirlari_Icindeyse_Bolge_Otomatik_Atanir()
    {
        using var dbContext = InBellekDbContextOlustur();
        var kategori = OrnekKategoriEkle(dbContext);

        var bolge = new Bolge(Guid.NewGuid(), "Merkez Mahalle", KareBolgeOlustur(41.0, 29.0, kenarUzunlugu: 0.1));
        dbContext.Bolgeler.Add(bolge);
        await dbContext.SaveChangesAsync();

        var servis = ServisOlustur(dbContext);
        var istek = new GorevTalebiOlusturIstegi("Kaldirim hasari", null, kategori.Id, 41.0, 29.0,
            GorevOnceligi.Normal, GorevKaynagi.OperatorGirisi, null, null);

        var sonuc = await servis.OlusturAsync(istek, olusturanPersonelId: null);

        Assert.Equal("Merkez Mahalle", sonuc.BolgeAdi);
    }

    [Fact]
    public async Task Olustur_Konum_Hicbir_Bolgeye_Girmiyorsa_BolgeAdi_Null_Kalir()
    {
        using var dbContext = InBellekDbContextOlustur();
        var kategori = OrnekKategoriEkle(dbContext);

        var uzakBolge = new Bolge(Guid.NewGuid(), "Uzak Mahalle", KareBolgeOlustur(10.0, 10.0, kenarUzunlugu: 0.1));
        dbContext.Bolgeler.Add(uzakBolge);
        await dbContext.SaveChangesAsync();

        var servis = ServisOlustur(dbContext);
        var istek = new GorevTalebiOlusturIstegi("Kaldirim hasari", null, kategori.Id, 41.0, 29.0,
            GorevOnceligi.Normal, GorevKaynagi.OperatorGirisi, null, null);

        var sonuc = await servis.OlusturAsync(istek, olusturanPersonelId: null);

        Assert.Null(sonuc.BolgeAdi);
    }

    [Fact]
    public async Task Olustur_Gecersiz_KategoriId_Ile_Istisna_Firlatir()
    {
        using var dbContext = InBellekDbContextOlustur();
        var servis = ServisOlustur(dbContext);

        var istek = new GorevTalebiOlusturIstegi("Baslik", null, Guid.NewGuid(), 41.0, 29.0,
            GorevOnceligi.Normal, GorevKaynagi.OperatorGirisi, null, null);

        await Assert.ThrowsAsync<GorevKategorisiBulunamadiException>(() => servis.OlusturAsync(istek, null));
    }

    [Fact]
    public async Task Olustur_Pasif_Kategori_Ile_Istisna_Firlatir()
    {
        using var dbContext = InBellekDbContextOlustur();
        var kategori = OrnekKategoriEkle(dbContext, aktif: false);
        var servis = ServisOlustur(dbContext);

        var istek = new GorevTalebiOlusturIstegi("Baslik", null, kategori.Id, 41.0, 29.0,
            GorevOnceligi.Normal, GorevKaynagi.OperatorGirisi, null, null);

        await Assert.ThrowsAsync<GorevKategorisiPasifException>(() => servis.OlusturAsync(istek, null));
    }

    [Fact]
    public async Task Listele_Durum_Filtresi_Sadece_Eslesen_Kayitlari_Doner()
    {
        using var dbContext = InBellekDbContextOlustur();
        var kategori = OrnekKategoriEkle(dbContext);

        var acikGorev = new GorevTalebi("Acik gorev", kategori, KonumFabrikasi.NoktaOlustur(41.0, 29.0),
            GorevOnceligi.Normal, GorevKaynagi.OperatorGirisi);
        var iptalGorev = new GorevTalebi("Iptal gorev", kategori, KonumFabrikasi.NoktaOlustur(41.0, 29.0),
            GorevOnceligi.Normal, GorevKaynagi.OperatorGirisi);
        iptalGorev.IptalEt(Guid.NewGuid(), "mukerrer kayit");

        dbContext.GorevTalepleri.AddRange(acikGorev, iptalGorev);
        await dbContext.SaveChangesAsync();

        var servis = ServisOlustur(dbContext);
        var sonuc = await servis.ListeleAsync(new GorevTalebiFiltre { Durum = GorevDurumu.Acildi });

        var kayit = Assert.Single(sonuc.Kayitlar);
        Assert.Equal("Acik gorev", kayit.Baslik);
    }

    [Fact]
    public async Task Listele_AtananEkipId_Filtresi_Sadece_O_Ekibe_Atanan_Gorevleri_Doner()
    {
        using var dbContext = InBellekDbContextOlustur();
        var kategori = OrnekKategoriEkle(dbContext);

        var birim = new Birim(Guid.NewGuid(), "Fen Isleri Mudurlugu");
        var ekipA = birim.EkipEkle("Ekip-A");
        var ekipB = birim.EkipEkle("Ekip-B");
        dbContext.Birimler.Add(birim);

        var ekibeAAtanan = new GorevTalebi("Ekip A gorevi", kategori, KonumFabrikasi.NoktaOlustur(41.0, 29.0),
            GorevOnceligi.Normal, GorevKaynagi.OperatorGirisi);
        ekibeAAtanan.EkibeAta(ekipA, Guid.NewGuid());

        var ekibeBAtanan = new GorevTalebi("Ekip B gorevi", kategori, KonumFabrikasi.NoktaOlustur(41.0, 29.0),
            GorevOnceligi.Normal, GorevKaynagi.OperatorGirisi);
        ekibeBAtanan.EkibeAta(ekipB, Guid.NewGuid());

        dbContext.GorevTalepleri.AddRange(ekibeAAtanan, ekibeBAtanan);
        await dbContext.SaveChangesAsync();

        var servis = ServisOlustur(dbContext);
        var sonuc = await servis.ListeleAsync(new GorevTalebiFiltre { AtananEkipId = ekipA.Id });

        var kayit = Assert.Single(sonuc.Kayitlar);
        Assert.Equal("Ekip A gorevi", kayit.Baslik);
    }

    [Fact]
    public async Task DetayGetir_Olmayan_Id_Ile_Istisna_Firlatir()
    {
        using var dbContext = InBellekDbContextOlustur();
        var servis = ServisOlustur(dbContext);

        await Assert.ThrowsAsync<GorevTalebiBulunamadiException>(() => servis.DetayGetirAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task IptalEt_Gorevin_Durumunu_Iptal_Yapar_Ve_Denetim_Izine_Islenir()
    {
        using var dbContext = InBellekDbContextOlustur();
        var kategori = OrnekKategoriEkle(dbContext);

        var gorev = new GorevTalebi("Iptal edilecek gorev", kategori, KonumFabrikasi.NoktaOlustur(41.0, 29.0),
            GorevOnceligi.Normal, GorevKaynagi.OperatorGirisi);
        dbContext.GorevTalepleri.Add(gorev);
        await dbContext.SaveChangesAsync();

        var servis = ServisOlustur(dbContext);
        await servis.IptalEtAsync(gorev.Id, "yanlis ihbar", Guid.NewGuid());

        var detay = await servis.DetayGetirAsync(gorev.Id);
        Assert.Equal(nameof(GorevDurumu.Iptal), detay.Durum);
    }

    [Fact]
    public async Task Yakinimdaki_Uzak_Ve_Kapanmis_Gorevleri_Disarida_Birakip_Yakindan_Uzaga_Sirali_Doner()
    {
        // Not: EF Core InMemory saglayicisi PostGIS'in kuresel (spherical) ST_DWithin/ST_Distance
        // hesaplamasini degil, duzlemsel (planar) derece-bazli mesafeyi kullanir. Bu test gercek
        // metre dogrulugu icin degil, filtreleme/siralama MANTIGININ dogrulugu icindir; gercek
        // PostGIS metre hassasiyeti Sprint 4'teki Testcontainers entegrasyon testinde dogrulanacaktir.
        using var dbContext = InBellekDbContextOlustur();
        var kategori = OrnekKategoriEkle(dbContext);

        var merkezEnlem = 41.0;
        var merkezBoylam = 29.0;

        var cokYakin = new GorevTalebi("Cok yakin", kategori, KonumFabrikasi.NoktaOlustur(merkezEnlem + 0.0001, merkezBoylam),
            GorevOnceligi.Normal, GorevKaynagi.OperatorGirisi);
        var birazUzak = new GorevTalebi("Biraz uzak", kategori, KonumFabrikasi.NoktaOlustur(merkezEnlem + 0.001, merkezBoylam),
            GorevOnceligi.Normal, GorevKaynagi.OperatorGirisi);
        var cokUzak = new GorevTalebi("Cok uzak", kategori, KonumFabrikasi.NoktaOlustur(merkezEnlem + 10, merkezBoylam),
            GorevOnceligi.Normal, GorevKaynagi.OperatorGirisi);
        var iptalEdilmisYakinGorev = new GorevTalebi("Iptal ama yakin", kategori,
            KonumFabrikasi.NoktaOlustur(merkezEnlem + 0.0001, merkezBoylam), GorevOnceligi.Normal, GorevKaynagi.OperatorGirisi);
        iptalEdilmisYakinGorev.IptalEt(Guid.NewGuid(), "test amacli iptal");

        dbContext.GorevTalepleri.AddRange(cokYakin, birazUzak, cokUzak, iptalEdilmisYakinGorev);
        await dbContext.SaveChangesAsync();

        var servis = ServisOlustur(dbContext);
        var sonuc = await servis.YakinimdakiGetirAsync(merkezEnlem, merkezBoylam, yaricapMetre: 0.01, maksimumSonucSayisi: 10);

        Assert.Equal(2, sonuc.Count);
        Assert.Equal("Cok yakin", sonuc[0].Baslik);
        Assert.Equal("Biraz uzak", sonuc[1].Baslik);
    }

    private static (Birim Birim, Ekip Ekip, Personel Personel) EkibeAtanmisPersonelOlustur(SahaGorDbContext dbContext)
    {
        var birim = new Birim(Guid.NewGuid(), "Fen Isleri Mudurlugu");
        var ekip = birim.EkipEkle("Ekip-1");
        var personel = birim.PersonelEkle("Ahmet Yilmaz", "ahmet.yilmaz", "hash", "5551112233",
            PersonelRolu.SahaPersoneli);
        ekip.UyeEkle(personel);

        dbContext.Birimler.Add(birim);
        return (birim, ekip, personel);
    }

    [Fact]
    public async Task YolaCik_Ekip_Uyesi_Icin_Durumu_YolaCikildi_Yapar()
    {
        using var dbContext = InBellekDbContextOlustur();
        var kategori = OrnekKategoriEkle(dbContext);
        var (_, ekip, personel) = EkibeAtanmisPersonelOlustur(dbContext);

        var gorev = new GorevTalebi("Kaldirim hasari", kategori, KonumFabrikasi.NoktaOlustur(41.0, 29.0),
            GorevOnceligi.Normal, GorevKaynagi.OperatorGirisi);
        gorev.EkibeAta(ekip, Guid.NewGuid());
        dbContext.GorevTalepleri.Add(gorev);
        await dbContext.SaveChangesAsync();

        var servis = ServisOlustur(dbContext);
        var sonuc = await servis.YolaCikAsync(gorev.Id, personel.Id);

        Assert.Equal(nameof(GorevDurumu.YolaCikildi), sonuc.Durum);
    }

    [Fact]
    public async Task YolaCik_Baska_Ekibin_Uyesi_Icin_ErisimYetkisiYokException_Firlatir()
    {
        using var dbContext = InBellekDbContextOlustur();
        var kategori = OrnekKategoriEkle(dbContext);
        var (birim, ekip, _) = EkibeAtanmisPersonelOlustur(dbContext);
        var baskaEkip = birim.EkipEkle("Ekip-2");
        var baskaEkipUyesi = birim.PersonelEkle("Mehmet Demir", "mehmet.demir", "hash", "5559998877",
            PersonelRolu.SahaPersoneli);
        baskaEkip.UyeEkle(baskaEkipUyesi);

        var gorev = new GorevTalebi("Kaldirim hasari", kategori, KonumFabrikasi.NoktaOlustur(41.0, 29.0),
            GorevOnceligi.Normal, GorevKaynagi.OperatorGirisi);
        gorev.EkibeAta(ekip, Guid.NewGuid());
        dbContext.GorevTalepleri.Add(gorev);
        await dbContext.SaveChangesAsync();

        var servis = ServisOlustur(dbContext);

        await Assert.ThrowsAsync<GorevErisimYetkisiYokException>(() => servis.YolaCikAsync(gorev.Id, baskaEkipUyesi.Id));
    }

    [Fact]
    public async Task Baslat_Yanlis_Durumdan_Cagrilinca_GorevDurumCakismasiException_Firlatir_Ve_Mevcut_Durumu_Bildirir()
    {
        using var dbContext = InBellekDbContextOlustur();
        var kategori = OrnekKategoriEkle(dbContext);
        var (_, ekip, personel) = EkibeAtanmisPersonelOlustur(dbContext);

        var gorev = new GorevTalebi("Kaldirim hasari", kategori, KonumFabrikasi.NoktaOlustur(41.0, 29.0),
            GorevOnceligi.Normal, GorevKaynagi.OperatorGirisi);
        gorev.EkibeAta(ekip, Guid.NewGuid());
        // Bilerek "YolaCik" adimi atlanir: gorev hala "Atandi" durumunda, "Baslat" gecersiz olmali.
        dbContext.GorevTalepleri.Add(gorev);
        await dbContext.SaveChangesAsync();

        var servis = ServisOlustur(dbContext);

        var hata = await Assert.ThrowsAsync<GorevDurumCakismasiException>(() => servis.BaslatAsync(gorev.Id, personel.Id));
        Assert.Equal(nameof(GorevDurumu.Atandi), hata.MevcutDurum);
    }

    [Fact]
    public async Task FotografEkle_Ekip_Uyesi_Icin_Fotografi_Gorevin_Fotograflarina_Ekler()
    {
        using var dbContext = InBellekDbContextOlustur();
        var kategori = OrnekKategoriEkle(dbContext);
        var (_, ekip, personel) = EkibeAtanmisPersonelOlustur(dbContext);

        var gorev = new GorevTalebi("Kaldirim hasari", kategori, KonumFabrikasi.NoktaOlustur(41.0, 29.0),
            GorevOnceligi.Normal, GorevKaynagi.OperatorGirisi);
        gorev.EkibeAta(ekip, Guid.NewGuid());
        dbContext.GorevTalepleri.Add(gorev);
        await dbContext.SaveChangesAsync();

        var servis = ServisOlustur(dbContext);
        using var sahteIcerik = new MemoryStream([1, 2, 3]);

        var yanit = await servis.FotografEkleAsync(gorev.Id, personel.Id, sahteIcerik, ".jpg",
            GorevFotografAsamasi.Sonra, 41.0, 29.0, DateTime.UtcNow);

        Assert.Equal(nameof(GorevFotografAsamasi.Sonra), yanit.Asama);

        var detay = await servis.DetayGetirAsync(gorev.Id);
        Assert.Single(detay.Fotograflar);
    }

    [Fact]
    public async Task FotografEkle_Baska_Ekibin_Uyesi_Icin_ErisimYetkisiYokException_Firlatir()
    {
        using var dbContext = InBellekDbContextOlustur();
        var kategori = OrnekKategoriEkle(dbContext);
        var (birim, ekip, _) = EkibeAtanmisPersonelOlustur(dbContext);
        var baskaEkip = birim.EkipEkle("Ekip-2");
        var baskaEkipUyesi = birim.PersonelEkle("Mehmet Demir", "mehmet.demir2", "hash", "5559998877",
            PersonelRolu.SahaPersoneli);
        baskaEkip.UyeEkle(baskaEkipUyesi);

        var gorev = new GorevTalebi("Kaldirim hasari", kategori, KonumFabrikasi.NoktaOlustur(41.0, 29.0),
            GorevOnceligi.Normal, GorevKaynagi.OperatorGirisi);
        gorev.EkibeAta(ekip, Guid.NewGuid());
        dbContext.GorevTalepleri.Add(gorev);
        await dbContext.SaveChangesAsync();

        var servis = ServisOlustur(dbContext);
        using var sahteIcerik = new MemoryStream([1, 2, 3]);

        await Assert.ThrowsAsync<GorevErisimYetkisiYokException>(() =>
            servis.FotografEkleAsync(gorev.Id, baskaEkipUyesi.Id, sahteIcerik, ".jpg",
                GorevFotografAsamasi.Sonra, 41.0, 29.0, DateTime.UtcNow));
    }

    [Fact]
    public async Task Olustur_Uygun_Ekip_Yoksa_Gorev_Otomatik_Atanamadi_Olarak_Isaretlenir()
    {
        using var dbContext = InBellekDbContextOlustur();
        var kategori = OrnekKategoriEkle(dbContext);
        var servis = ServisOlustur(dbContext);

        // Bilerek hicbir ekip olusturulmadi: atama motoru "uygun ekip yok" sonucuna varmali.
        var istek = new GorevTalebiOlusturIstegi("Kaldirim hasari", null, kategori.Id, 41.0, 29.0,
            GorevOnceligi.Normal, GorevKaynagi.OperatorGirisi, null, null);

        var sonuc = await servis.OlusturAsync(istek, olusturanPersonelId: null);

        Assert.Equal(nameof(GorevDurumu.Atanamadi), sonuc.Durum);
    }

    [Fact]
    public async Task Olustur_Uygun_Ekip_Varsa_Sistem_Otomatik_Atama_YAPMAZ_Acildi_Durumunda_Kalir()
    {
        using var dbContext = InBellekDbContextOlustur();
        var kategori = OrnekKategoriEkle(dbContext);
        var (_, ekip, personel) = EkibeAtanmisPersonelOlustur(dbContext);
        ekip.UzmanlikAlaniEkle(kategori);
        personel.MusaitlikDurumunuGuncelle(true);
        await dbContext.SaveChangesAsync();

        var servis = ServisOlustur(dbContext);
        var istek = new GorevTalebiOlusturIstegi("Kaldirim hasari", null, kategori.Id, 41.0, 29.0,
            GorevOnceligi.Normal, GorevKaynagi.OperatorGirisi, null, null);

        var sonuc = await servis.OlusturAsync(istek, olusturanPersonelId: null);

        // SG-311: uygun bir aday olsa dahi sistem ASLA otomatik atamaz; Amir'in onayi gerekir.
        Assert.Equal(nameof(GorevDurumu.Acildi), sonuc.Durum);
    }

    [Fact]
    public async Task AtaAsync_Gecerli_Ekiple_Cagrilinca_Gorevi_Atandi_Durumuna_Gecirir()
    {
        using var dbContext = InBellekDbContextOlustur();
        var kategori = OrnekKategoriEkle(dbContext);
        var (_, ekip, personel) = EkibeAtanmisPersonelOlustur(dbContext);

        var gorev = new GorevTalebi("Kaldirim hasari", kategori, KonumFabrikasi.NoktaOlustur(41.0, 29.0),
            GorevOnceligi.Normal, GorevKaynagi.OperatorGirisi);
        dbContext.GorevTalepleri.Add(gorev);
        await dbContext.SaveChangesAsync();

        var servis = ServisOlustur(dbContext);
        var sonuc = await servis.AtaAsync(gorev.Id, ekip.Id, personel.Id, atayanPersonelId: Guid.NewGuid());

        Assert.Equal(nameof(GorevDurumu.Atandi), sonuc.Durum);
        Assert.Equal(ekip.Ad, sonuc.AtananEkipAdi);
    }

    [Fact]
    public async Task AtaAsync_Zaten_Atanmis_Bir_Gorev_Icin_Cakisma_Firlatir()
    {
        using var dbContext = InBellekDbContextOlustur();
        var kategori = OrnekKategoriEkle(dbContext);
        var (_, ekip, _) = EkibeAtanmisPersonelOlustur(dbContext);

        var gorev = new GorevTalebi("Kaldirim hasari", kategori, KonumFabrikasi.NoktaOlustur(41.0, 29.0),
            GorevOnceligi.Normal, GorevKaynagi.OperatorGirisi);
        gorev.EkibeAta(ekip, Guid.NewGuid());
        dbContext.GorevTalepleri.Add(gorev);
        await dbContext.SaveChangesAsync();

        var servis = ServisOlustur(dbContext);

        await Assert.ThrowsAsync<GorevDurumCakismasiException>(() =>
            servis.AtaAsync(gorev.Id, ekip.Id, null, atayanPersonelId: Guid.NewGuid()));
    }

    [Fact]
    public async Task IstatistikleriGetir_Acik_Ve_Tamamlanmis_Gorevleri_Dogru_Sayar()
    {
        using var dbContext = InBellekDbContextOlustur();
        var kategori = OrnekKategoriEkle(dbContext);
        var (_, ekip, personel) = EkibeAtanmisPersonelOlustur(dbContext);

        var acikGorev = new GorevTalebi("Acik gorev", kategori, KonumFabrikasi.NoktaOlustur(41.0, 29.0),
            GorevOnceligi.Normal, GorevKaynagi.OperatorGirisi);

        var tamamlananGorev = new GorevTalebi("Tamamlanan gorev", kategori, KonumFabrikasi.NoktaOlustur(41.0, 29.0),
            GorevOnceligi.Normal, GorevKaynagi.OperatorGirisi);
        tamamlananGorev.EkibeAta(ekip, Guid.NewGuid());
        tamamlananGorev.YolaCik(personel.Id);
        tamamlananGorev.Baslat(personel.Id);
        tamamlananGorev.FotografEkle("kanit.jpg", GorevFotografAsamasi.Sonra, KonumFabrikasi.NoktaOlustur(41.0, 29.0),
            DateTime.UtcNow, personel.Id);
        tamamlananGorev.Tamamla(personel.Id);

        dbContext.GorevTalepleri.AddRange(acikGorev, tamamlananGorev);
        await dbContext.SaveChangesAsync();

        var servis = ServisOlustur(dbContext);
        var istatistikler = await servis.IstatistikleriGetirAsync(gunSayisi: 7);

        Assert.Equal(1, istatistikler.AcikGorevSayisi);
        Assert.Equal(1, istatistikler.PencereIcindeTamamlananGorevSayisi);
        Assert.NotNull(istatistikler.OrtalamaCozumSuresiDakika);
    }

    private static GorevTalebi TamamlanmisGorevOlustur(SahaGorDbContext dbContext, Ekip ekip, Personel personel,
        GorevKategorisi kategori, string? bildirenTelefonNumarasi = null)
    {
        var gorev = new GorevTalebi("Kaldirim hasari", kategori, KonumFabrikasi.NoktaOlustur(41.0, 29.0),
            GorevOnceligi.Normal, GorevKaynagi.OperatorGirisi, bildirenTelefonNumarasi: bildirenTelefonNumarasi);
        gorev.EkibeAta(ekip, Guid.NewGuid());
        gorev.YolaCik(personel.Id);
        gorev.Baslat(personel.Id);
        gorev.FotografEkle("kanit.jpg", GorevFotografAsamasi.Sonra, KonumFabrikasi.NoktaOlustur(41.0, 29.0),
            DateTime.UtcNow, personel.Id);
        gorev.Tamamla(personel.Id);

        dbContext.GorevTalepleri.Add(gorev);
        return gorev;
    }

    [Fact]
    public async Task DogrulaAsync_Tamamlanmis_Gorevi_Dogrulandi_Durumuna_Gecirir()
    {
        using var dbContext = InBellekDbContextOlustur();
        var kategori = OrnekKategoriEkle(dbContext);
        var (_, ekip, personel) = EkibeAtanmisPersonelOlustur(dbContext);
        var gorev = TamamlanmisGorevOlustur(dbContext, ekip, personel, kategori);
        await dbContext.SaveChangesAsync();

        var servis = ServisOlustur(dbContext);
        var sonuc = await servis.DogrulaAsync(gorev.Id, dogrulayanPersonelId: Guid.NewGuid());

        Assert.Equal(nameof(GorevDurumu.Dogrulandi), sonuc.Durum);
    }

    [Fact]
    public async Task DogrulaAsync_Bildiren_Telefonu_Varsa_Vatandasa_Bilgilendirme_SMS_Gonderir()
    {
        using var dbContext = InBellekDbContextOlustur();
        var kategori = OrnekKategoriEkle(dbContext);
        var (_, ekip, personel) = EkibeAtanmisPersonelOlustur(dbContext);
        var gorev = TamamlanmisGorevOlustur(dbContext, ekip, personel, kategori, bildirenTelefonNumarasi: "5551234567");
        await dbContext.SaveChangesAsync();

        var sahteSms = new SahteSmsGonderimServisi();
        var servis = ServisOlustur(dbContext, sahteSms);

        await servis.DogrulaAsync(gorev.Id, dogrulayanPersonelId: Guid.NewGuid());

        var gonderilenSms = Assert.Single(sahteSms.GonderilenSmsler);
        Assert.Equal("5551234567", gonderilenSms.TelefonNumarasi);
    }

    [Fact]
    public async Task DogrulaAsync_Bildiren_Telefonu_Yoksa_SMS_Gondermez()
    {
        using var dbContext = InBellekDbContextOlustur();
        var kategori = OrnekKategoriEkle(dbContext);
        var (_, ekip, personel) = EkibeAtanmisPersonelOlustur(dbContext);
        var gorev = TamamlanmisGorevOlustur(dbContext, ekip, personel, kategori, bildirenTelefonNumarasi: null);
        await dbContext.SaveChangesAsync();

        var sahteSms = new SahteSmsGonderimServisi();
        var servis = ServisOlustur(dbContext, sahteSms);

        await servis.DogrulaAsync(gorev.Id, dogrulayanPersonelId: Guid.NewGuid());

        Assert.Empty(sahteSms.GonderilenSmsler);
    }

    [Fact]
    public async Task DogrulaAsync_Henuz_Tamamlanmamis_Gorev_Icin_GorevDurumCakismasiException_Firlatir()
    {
        using var dbContext = InBellekDbContextOlustur();
        var kategori = OrnekKategoriEkle(dbContext);
        var (_, ekip, personel) = EkibeAtanmisPersonelOlustur(dbContext);

        var gorev = new GorevTalebi("Kaldirim hasari", kategori, KonumFabrikasi.NoktaOlustur(41.0, 29.0),
            GorevOnceligi.Normal, GorevKaynagi.OperatorGirisi);
        gorev.EkibeAta(ekip, Guid.NewGuid());
        // Bilerek "YolaCik/Baslat/Tamamla" adimlari atlanir: gorev hala "Atandi" durumunda.
        dbContext.GorevTalepleri.Add(gorev);
        await dbContext.SaveChangesAsync();

        var servis = ServisOlustur(dbContext);

        var hata = await Assert.ThrowsAsync<GorevDurumCakismasiException>(() =>
            servis.DogrulaAsync(gorev.Id, dogrulayanPersonelId: Guid.NewGuid()));
        Assert.Equal(nameof(GorevDurumu.Atandi), hata.MevcutDurum);
    }

    [Fact]
    public async Task SahayaGeriGonderAsync_Tamamlanmis_Gorevi_Baslandi_Durumuna_Geri_Alir()
    {
        using var dbContext = InBellekDbContextOlustur();
        var kategori = OrnekKategoriEkle(dbContext);
        var (_, ekip, personel) = EkibeAtanmisPersonelOlustur(dbContext);
        var gorev = TamamlanmisGorevOlustur(dbContext, ekip, personel, kategori);
        await dbContext.SaveChangesAsync();

        var servis = ServisOlustur(dbContext);
        var sonuc = await servis.SahayaGeriGonderAsync(gorev.Id, amirId: Guid.NewGuid(), neden: "Fotograf net degil");

        Assert.Equal(nameof(GorevDurumu.Baslandi), sonuc.Durum);
    }

    [Fact]
    public async Task SahayaGeriGonderAsync_Tamamlanmamis_Gorev_Icin_GorevDurumCakismasiException_Firlatir()
    {
        using var dbContext = InBellekDbContextOlustur();
        var kategori = OrnekKategoriEkle(dbContext);
        var (_, ekip, personel) = EkibeAtanmisPersonelOlustur(dbContext);

        var gorev = new GorevTalebi("Kaldirim hasari", kategori, KonumFabrikasi.NoktaOlustur(41.0, 29.0),
            GorevOnceligi.Normal, GorevKaynagi.OperatorGirisi);
        gorev.EkibeAta(ekip, Guid.NewGuid());
        gorev.YolaCik(personel.Id);
        // Bilerek "Baslat/Tamamla" adimlari atlanir: gorev hala "YolaCikildi" durumunda.
        dbContext.GorevTalepleri.Add(gorev);
        await dbContext.SaveChangesAsync();

        var servis = ServisOlustur(dbContext);

        var hata = await Assert.ThrowsAsync<GorevDurumCakismasiException>(() =>
            servis.SahayaGeriGonderAsync(gorev.Id, amirId: Guid.NewGuid(), neden: "Eksik kanit"));
        Assert.Equal(nameof(GorevDurumu.YolaCikildi), hata.MevcutDurum);
    }
}
