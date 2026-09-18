using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SahaGor.Application.Bildirimler;
using SahaGor.Domain.Enumlar;
using SahaGor.Domain.Ortak;
using SahaGor.Domain.Varliklar;
using SahaGor.Infrastructure.Gorevler;
using SahaGor.Infrastructure.Persistence;

namespace SahaGor.UnitTests.Gorevler;

/// <summary>
/// SG-410/411/412 kabul kriterlerini dogrular: acik gorevler SLA esiklerine gore taranir,
/// esik/ihlal basina EN FAZLA BIR KEZ alarm uretilir ve kapali gorevler taranmaz.
/// </summary>
public class SlaAlarmTarayiciServisiTests
{
    /// <summary>Testlerde gercek SignalR altyapisina ihtiyac olmadan gonderilen alarmlari kaydeden sahte yayinlayici.</summary>
    private sealed class SahteSlaAlarmYayinlayici : ISlaAlarmYayinlayici
    {
        public List<SlaAlarmBildirimi> GonderilenAlarmlar { get; } = new();

        public Task YayinlaAsync(SlaAlarmBildirimi bildirim, CancellationToken iptalToken = default)
        {
            GonderilenAlarmlar.Add(bildirim);
            return Task.CompletedTask;
        }
    }

    private static SahaGorDbContext InBellekDbContextOlustur() =>
        new(new DbContextOptionsBuilder<SahaGorDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static SlaAlarmTarayiciServisi ServisOlustur(SahaGorDbContext dbContext,
        SahteSlaAlarmYayinlayici yayinlayici, double yaklasmaEsikYuzdesi = 80) =>
        new(dbContext, yayinlayici, Options.Create(new SlaAlarmAyarlari { YaklasmaEsikYuzdesi = yaklasmaEsikYuzdesi }),
            NullLogger<SlaAlarmTarayiciServisi>.Instance);

    private static GorevKategorisi OrnekKategoriEkle(SahaGorDbContext dbContext, int slaCozumSuresiDakika = 100)
    {
        var kategori = new GorevKategorisi("Kırık Kaldırım", slaYanitSuresiDakika: 30,
            slaCozumSuresiDakika: slaCozumSuresiDakika);
        dbContext.GorevKategorileri.Add(kategori);
        dbContext.SaveChanges();
        return kategori;
    }

    /// <summary>
    /// GorevTalebi'nin olusturulma/SLA hedef zamanlari kurucuda "DateTime.UtcNow" ile
    /// sabitlenir ve disaridan enjekte edilemez (kodun geri kalaninda oldugu gibi burada da
    /// bilerek bir "sahte saat" soyutlamasi yok). Testlerin dakikalarca gercekten beklemesini
    /// onlemek icin, sadece bu test dosyasinda, zamanlari geriye almak amaciyla reflection
    /// kullanilir; bu private setter'lari .NET Core'da normal sekilde cagirir.
    /// </summary>
    private static void GorevZamanlariniGeriAl(GorevTalebi gorev, DateTime olusturulmaZamaniUtc, DateTime slaHedefZamaniUtc)
    {
        SetPrivate(gorev, nameof(GorevTalebi.OlusturulmaZamaniUtc), olusturulmaZamaniUtc);
        SetPrivate(gorev, nameof(GorevTalebi.SlaHedefZamaniUtc), slaHedefZamaniUtc);
    }

    private static void SetPrivate(object hedef, string ozellikAdi, object deger)
    {
        var ozellik = hedef.GetType().GetProperty(ozellikAdi, BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException($"'{ozellikAdi}' ozelligi bulunamadi.");
        ozellik.GetSetMethod(nonPublic: true)!.Invoke(hedef, new[] { deger });
    }

    [Fact]
    public async Task Tarama_Esigi_Asmis_Ama_Henuz_Ihlal_Etmemis_Gorev_Icin_Yaklasma_Alarmi_Gonderir()
    {
        using var dbContext = InBellekDbContextOlustur();
        var kategori = OrnekKategoriEkle(dbContext, slaCozumSuresiDakika: 100);

        var gorev = new GorevTalebi("Kaldirim hasari", kategori, KonumFabrikasi.NoktaOlustur(41.0, 29.0),
            GorevOnceligi.Normal, GorevKaynagi.OperatorGirisi);
        var simdi = DateTime.UtcNow;
        // %85 tuketim: hala acik ama esik (%80) asilmis, henuz ihlal edilmemis (hedef gelecekte).
        GorevZamanlariniGeriAl(gorev, simdi.AddMinutes(-85), simdi.AddMinutes(15));
        dbContext.GorevTalepleri.Add(gorev);
        await dbContext.SaveChangesAsync();

        var yayinlayici = new SahteSlaAlarmYayinlayici();
        var servis = ServisOlustur(dbContext, yayinlayici);

        var gonderilenSayi = await servis.TaraVeAlarmlariGonderAsync();

        Assert.Equal(1, gonderilenSayi);
        var alarm = Assert.Single(yayinlayici.GonderilenAlarmlar);
        Assert.Equal(SlaAlarmTuru.Yaklasiyor, alarm.AlarmTuru);
        Assert.Equal(gorev.Id, alarm.GorevId);

        var guncelGorev = await dbContext.GorevTalepleri.SingleAsync(g => g.Id == gorev.Id);
        Assert.NotNull(guncelGorev.SlaYaklasmaAlarmiZamaniUtc);
        Assert.Null(guncelGorev.SlaIhlalAlarmiZamaniUtc);
    }

    [Fact]
    public async Task Tarama_Ayni_Gorevi_Ikinci_Kez_Taradiginda_Tekrar_Alarm_Gondermez()
    {
        using var dbContext = InBellekDbContextOlustur();
        var kategori = OrnekKategoriEkle(dbContext, slaCozumSuresiDakika: 100);

        var gorev = new GorevTalebi("Kaldirim hasari", kategori, KonumFabrikasi.NoktaOlustur(41.0, 29.0),
            GorevOnceligi.Normal, GorevKaynagi.OperatorGirisi);
        var simdi = DateTime.UtcNow;
        GorevZamanlariniGeriAl(gorev, simdi.AddMinutes(-85), simdi.AddMinutes(15));
        dbContext.GorevTalepleri.Add(gorev);
        await dbContext.SaveChangesAsync();

        var yayinlayici = new SahteSlaAlarmYayinlayici();
        var servis = ServisOlustur(dbContext, yayinlayici);

        var ilkTarama = await servis.TaraVeAlarmlariGonderAsync();
        var ikinciTarama = await servis.TaraVeAlarmlariGonderAsync();

        Assert.Equal(1, ilkTarama);
        Assert.Equal(0, ikinciTarama);
        Assert.Single(yayinlayici.GonderilenAlarmlar);
    }

    [Fact]
    public async Task Tarama_Sla_Hedefi_Asilmis_Acik_Gorev_Icin_Ihlal_Alarmi_Gonderir()
    {
        using var dbContext = InBellekDbContextOlustur();
        var kategori = OrnekKategoriEkle(dbContext, slaCozumSuresiDakika: 100);

        var gorev = new GorevTalebi("Kaldirim hasari", kategori, KonumFabrikasi.NoktaOlustur(41.0, 29.0),
            GorevOnceligi.Normal, GorevKaynagi.OperatorGirisi);
        var simdi = DateTime.UtcNow;
        // Hedef 50 dakika once gecmis: SLA ihlal edilmis.
        GorevZamanlariniGeriAl(gorev, simdi.AddMinutes(-150), simdi.AddMinutes(-50));
        dbContext.GorevTalepleri.Add(gorev);
        await dbContext.SaveChangesAsync();

        var yayinlayici = new SahteSlaAlarmYayinlayici();
        var servis = ServisOlustur(dbContext, yayinlayici);

        var gonderilenSayi = await servis.TaraVeAlarmlariGonderAsync();

        Assert.Equal(1, gonderilenSayi);
        var alarm = Assert.Single(yayinlayici.GonderilenAlarmlar);
        Assert.Equal(SlaAlarmTuru.Ihlal, alarm.AlarmTuru);

        var guncelGorev = await dbContext.GorevTalepleri.SingleAsync(g => g.Id == gorev.Id);
        Assert.NotNull(guncelGorev.SlaIhlalAlarmiZamaniUtc);
    }

    [Fact]
    public async Task Tarama_Kapatilmis_Gorevleri_Sla_Asimis_Olsalar_Dahi_Alarma_Dahil_Etmez()
    {
        using var dbContext = InBellekDbContextOlustur();
        var kategori = OrnekKategoriEkle(dbContext, slaCozumSuresiDakika: 100);

        var gorev = new GorevTalebi("Kaldirim hasari", kategori, KonumFabrikasi.NoktaOlustur(41.0, 29.0),
            GorevOnceligi.Normal, GorevKaynagi.OperatorGirisi);
        var simdi = DateTime.UtcNow;
        GorevZamanlariniGeriAl(gorev, simdi.AddMinutes(-150), simdi.AddMinutes(-50));
        gorev.IptalEt(Guid.NewGuid(), "mukerrer kayit");
        dbContext.GorevTalepleri.Add(gorev);
        await dbContext.SaveChangesAsync();

        var yayinlayici = new SahteSlaAlarmYayinlayici();
        var servis = ServisOlustur(dbContext, yayinlayici);

        var gonderilenSayi = await servis.TaraVeAlarmlariGonderAsync();

        Assert.Equal(0, gonderilenSayi);
        Assert.Empty(yayinlayici.GonderilenAlarmlar);
    }

    [Fact]
    public async Task Tarama_Esigin_Altindaki_Yeni_Gorev_Icin_Alarm_Uretmez()
    {
        using var dbContext = InBellekDbContextOlustur();
        var kategori = OrnekKategoriEkle(dbContext, slaCozumSuresiDakika: 100);

        var gorev = new GorevTalebi("Kaldirim hasari", kategori, KonumFabrikasi.NoktaOlustur(41.0, 29.0),
            GorevOnceligi.Normal, GorevKaynagi.OperatorGirisi);
        dbContext.GorevTalepleri.Add(gorev);
        await dbContext.SaveChangesAsync();

        var yayinlayici = new SahteSlaAlarmYayinlayici();
        var servis = ServisOlustur(dbContext, yayinlayici);

        var gonderilenSayi = await servis.TaraVeAlarmlariGonderAsync();

        Assert.Equal(0, gonderilenSayi);
        Assert.Empty(yayinlayici.GonderilenAlarmlar);
    }
}
