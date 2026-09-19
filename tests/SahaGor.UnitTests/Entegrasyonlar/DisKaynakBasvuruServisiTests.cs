using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SahaGor.Application.Bildirimler;
using SahaGor.Application.Entegrasyonlar.Dtolar;
using SahaGor.Application.Entegrasyonlar.Istisnalar;
using SahaGor.Application.Gorevler;
using SahaGor.Domain.Enumlar;
using SahaGor.Domain.Varliklar;
using SahaGor.Infrastructure.Atama;
using SahaGor.Infrastructure.Entegrasyonlar;
using SahaGor.Infrastructure.Gorevler;
using SahaGor.Infrastructure.Persistence;
using SahaGor.Infrastructure.Sms;

namespace SahaGor.UnitTests.Entegrasyonlar;

public class DisKaynakBasvuruServisiTests
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

    private static SahaGorDbContext InBellekDbContextOlustur() =>
        new(new DbContextOptionsBuilder<SahaGorDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static DisKaynakBasvuruServisi ServisOlustur(SahaGorDbContext dbContext)
    {
        var gorevTalebiServisi = new GorevTalebiServisi(dbContext, new SahteGorevBildirimYayinlayici(),
            new SahteFotografDepolamaServisi(), new AtamaMotoru(dbContext, NullLogger<AtamaMotoru>.Instance),
            new LoglayanSmsGonderimServisi(NullLogger<LoglayanSmsGonderimServisi>.Instance),
            NullLogger<GorevTalebiServisi>.Instance);

        return new DisKaynakBasvuruServisi(dbContext, gorevTalebiServisi, NullLogger<DisKaynakBasvuruServisi>.Instance);
    }

    private static GorevKategorisi OrnekKategoriEkle(SahaGorDbContext dbContext, string ad = "Kırık Kaldırım")
    {
        var kategori = new GorevKategorisi(ad, slaYanitSuresiDakika: 60, slaCozumSuresiDakika: 480);
        dbContext.GorevKategorileri.Add(kategori);
        dbContext.SaveChanges();
        return kategori;
    }

    [Fact]
    public async Task Hat153BasvurusuIsleAsync_Gecerli_Basvuruyu_GorevTalebine_Cevirir()
    {
        using var dbContext = InBellekDbContextOlustur();
        OrnekKategoriEkle(dbContext, "Kırık Kaldırım");

        var servis = ServisOlustur(dbContext);
        var istek = new Hat153BasvuruIstegi("153-2026-000123", "Kaldirim cokmus", "Yaya gecidinde tehlike",
            41.0, 29.0, "5551234567", "Kırık Kaldırım");

        var sonuc = await servis.Hat153BasvurusuIsleAsync(istek);

        Assert.Equal("Kaldirim cokmus", sonuc.Baslik);
        Assert.Equal(nameof(GorevKaynagi.Hat153), sonuc.Kaynak);
        Assert.Equal(nameof(GorevDurumu.Atanamadi), sonuc.Durum);
    }

    [Fact]
    public async Task Hat153BasvurusuIsleAsync_Bilinmeyen_Kategori_Adiyla_Istisna_Firlatir()
    {
        using var dbContext = InBellekDbContextOlustur();
        var servis = ServisOlustur(dbContext);

        var istek = new Hat153BasvuruIstegi("153-2026-000124", "Baslik", null, 41.0, 29.0, null,
            "Olmayan Kategori");

        await Assert.ThrowsAsync<GorevKategorisiAdiylaBulunamadiException>(() =>
            servis.Hat153BasvurusuIsleAsync(istek));
    }

    [Fact]
    public async Task Hat153BasvurusuIsleAsync_Pasif_Kategoriyi_Aktif_Kategori_Gibi_Gormez()
    {
        using var dbContext = InBellekDbContextOlustur();
        var kategori = OrnekKategoriEkle(dbContext, "Pasif Kategori");
        kategori.Pasiflestir();
        await dbContext.SaveChangesAsync();

        var servis = ServisOlustur(dbContext);
        var istek = new Hat153BasvuruIstegi("153-2026-000125", "Baslik", null, 41.0, 29.0, null, "Pasif Kategori");

        await Assert.ThrowsAsync<GorevKategorisiAdiylaBulunamadiException>(() =>
            servis.Hat153BasvurusuIsleAsync(istek));
    }

    [Fact]
    public async Task Hat153BasvurusuIsleAsync_Ayni_ReferansNo_Ile_Tekrar_Cagrilinca_Mukerrer_Gorev_Olusturmaz()
    {
        // SG-423 (OWASP A04/A08): 153 sisteminin ag zaman asimi nedeniyle ayni basvuruyu
        // tekrar gondermesi (veya imzali bir istegin kotu niyetle tekrar oynatilmasi/replay)
        // ayni ReferansNo icin ikinci bir gorev OLUSTURMAMALI, mevcut olani dondurmelidir.
        using var dbContext = InBellekDbContextOlustur();
        OrnekKategoriEkle(dbContext, "Kırık Kaldırım");

        var servis = ServisOlustur(dbContext);
        var istek = new Hat153BasvuruIstegi("153-2026-TEKRAR-001", "Kaldirim cokmus", null,
            41.0, 29.0, "5551234567", "Kırık Kaldırım");

        var ilkSonuc = await servis.Hat153BasvurusuIsleAsync(istek);
        var ikinciSonuc = await servis.Hat153BasvurusuIsleAsync(istek);

        Assert.Equal(ilkSonuc.Id, ikinciSonuc.Id);
        Assert.Single(dbContext.GorevTalepleri);
    }
}
