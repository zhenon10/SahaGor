using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SahaGor.Application.Bildirimler;
using SahaGor.Application.Ekipler.Istisnalar;
using SahaGor.Application.Kurumlar.Dtolar;
using SahaGor.Application.Personeller.Dtolar;
using SahaGor.Application.Personeller.Istisnalar;
using SahaGor.Domain.Enumlar;
using SahaGor.Domain.Varliklar;
using SahaGor.Infrastructure.Ekipler;
using SahaGor.Infrastructure.Kimlik;
using SahaGor.Infrastructure.Kurumlar;
using SahaGor.Infrastructure.Personeller;
using SahaGor.Infrastructure.Persistence;

namespace SahaGor.UnitTests.Organizasyon;

public class OrganizasyonServisleriTests
{
    /// <summary>Testlerde gercek SignalR altyapisina ihtiyac olmadigindan kullanilan sahte (no-op) yayinlayici.</summary>
    private sealed class SahteEkipBildirimYayinlayici : IEkipBildirimYayinlayici
    {
        public Task YayinlaAsync(EkipKonumBildirimi bildirim, CancellationToken iptalToken = default) => Task.CompletedTask;
    }

    private static EkipServisi EkipServisiOlustur(SahaGorDbContext dbContext) =>
        new(dbContext, new SahteEkipBildirimYayinlayici(), NullLogger<EkipServisi>.Instance);

    private static SahaGorDbContext InBellekDbContextOlustur() =>
        new(new DbContextOptionsBuilder<SahaGorDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static (SahaGorDbContext DbContext, Birim Birim) BirimliOrtamHazirla()
    {
        var dbContext = InBellekDbContextOlustur();
        var kurum = new Kurum("Test Belediyesi");
        var birim = kurum.BirimEkle("Fen Isleri Mudurlugu");

        dbContext.Kurumlar.Add(kurum);
        dbContext.SaveChanges();

        return (dbContext, birim);
    }

    [Fact]
    public async Task Kurum_Olustur_Sonra_Listede_Gorunur()
    {
        using var dbContext = InBellekDbContextOlustur();
        var servis = new KurumServisi(dbContext, NullLogger<KurumServisi>.Instance);

        await servis.OlusturAsync(new KurumOlusturIstegi("Test Belediyesi", null, null));
        var liste = await servis.ListeleAsync();

        Assert.Single(liste);
        Assert.Equal("Test Belediyesi", liste[0].Ad);
    }

    [Fact]
    public async Task Personel_Olustur_Sifreyi_Hashler_Duz_Metin_Olarak_Saklamaz()
    {
        var (dbContext, birim) = BirimliOrtamHazirla();
        var sifreHashleyici = new BcryptSifreHashleyici();
        var servis = new PersonelServisi(dbContext, sifreHashleyici, NullLogger<PersonelServisi>.Instance);

        var istek = new PersonelOlusturIstegi(birim.Id, "Ahmet Yilmaz", "ahmet.yilmaz", "GucluSifre!2026",
            "5551112233", PersonelRolu.SahaPersoneli, null);

        var yanit = await servis.OlusturAsync(istek);

        var kaydedilenPersonel = await dbContext.Personeller.SingleAsync(p => p.Id == yanit.Id);
        Assert.NotEqual("GucluSifre!2026", kaydedilenPersonel.SifreHash);
        Assert.True(sifreHashleyici.Dogrula("GucluSifre!2026", kaydedilenPersonel.SifreHash));
    }

    [Fact]
    public async Task Personel_Olustur_Ayni_KullaniciAdi_Ile_Istisna_Firlatir()
    {
        var (dbContext, birim) = BirimliOrtamHazirla();
        var servis = new PersonelServisi(dbContext, new BcryptSifreHashleyici(), NullLogger<PersonelServisi>.Instance);

        var istek = new PersonelOlusturIstegi(birim.Id, "Ahmet Yilmaz", "ahmet.yilmaz", "GucluSifre!2026",
            "5551112233", PersonelRolu.SahaPersoneli, null);

        await servis.OlusturAsync(istek);

        await Assert.ThrowsAsync<KullaniciAdiZatenKullanimdaException>(() => servis.OlusturAsync(istek));
    }

    [Fact]
    public async Task Personel_SifreSifirla_Yeni_Sifreyle_Dogrulama_Basarili_Olur()
    {
        var (dbContext, birim) = BirimliOrtamHazirla();
        var sifreHashleyici = new BcryptSifreHashleyici();
        var servis = new PersonelServisi(dbContext, sifreHashleyici, NullLogger<PersonelServisi>.Instance);

        var yanit = await servis.OlusturAsync(new PersonelOlusturIstegi(birim.Id, "Ahmet Yilmaz", "ahmet.yilmaz",
            "EskiSifre!2026", "5551112233", PersonelRolu.SahaPersoneli, null));

        await servis.SifreSifirlaAsync(yanit.Id, new SifreSifirlaIstegi("YeniSifre!2026"));

        var guncelPersonel = await dbContext.Personeller.SingleAsync(p => p.Id == yanit.Id);
        Assert.True(sifreHashleyici.Dogrula("YeniSifre!2026", guncelPersonel.SifreHash));
        Assert.False(sifreHashleyici.Dogrula("EskiSifre!2026", guncelPersonel.SifreHash));
    }

    [Fact]
    public async Task Ekip_UyeEkle_SahaPersoneli_Olmayan_Rol_Icin_Islem_Gecersiz_Doner()
    {
        var (dbContext, birim) = BirimliOrtamHazirla();
        var ekip = birim.EkipEkle("Ekip-1");
        var amir = birim.PersonelEkle("Bir Amir", "bir.amir", "hash", "5550000000", PersonelRolu.Amir);

        dbContext.Ekipler.Add(ekip);
        dbContext.Personeller.Add(amir);
        await dbContext.SaveChangesAsync();

        var servis = EkipServisiOlustur(dbContext);

        await Assert.ThrowsAsync<InvalidOperationException>(() => servis.UyeEkleAsync(ekip.Id, amir.Id));
    }

    [Fact]
    public async Task Ekip_UyeEkle_Ve_UyeCikar_Beklenen_Sekilde_Calisir()
    {
        var (dbContext, birim) = BirimliOrtamHazirla();
        var ekip = birim.EkipEkle("Ekip-1");
        var sahaPersoneli = birim.PersonelEkle("Saha Personeli", "saha.personeli", "hash", "5550000001",
            PersonelRolu.SahaPersoneli);

        dbContext.Ekipler.Add(ekip);
        dbContext.Personeller.Add(sahaPersoneli);
        await dbContext.SaveChangesAsync();

        var servis = EkipServisiOlustur(dbContext);

        var eklendikten = await servis.UyeEkleAsync(ekip.Id, sahaPersoneli.Id);
        Assert.Single(eklendikten.Uyeler);

        var cikarildiktan = await servis.UyeCikarAsync(ekip.Id, sahaPersoneli.Id);
        Assert.Empty(cikarildiktan.Uyeler);
    }

    [Fact]
    public async Task Ekip_Olmayan_Id_Ile_Islem_EkipBulunamadiException_Firlatir()
    {
        using var dbContext = InBellekDbContextOlustur();
        var servis = EkipServisiOlustur(dbContext);

        await Assert.ThrowsAsync<EkipBulunamadiException>(() => servis.DetayGetirAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task Ekip_KonumGuncelle_Uye_Icin_Konumu_Gunceller()
    {
        var (dbContext, birim) = BirimliOrtamHazirla();
        var ekip = birim.EkipEkle("Ekip-1");
        var sahaPersoneli = birim.PersonelEkle("Saha Personeli", "saha.personeli2", "hash", "5550000002",
            PersonelRolu.SahaPersoneli);
        ekip.UyeEkle(sahaPersoneli);

        dbContext.Ekipler.Add(ekip);
        await dbContext.SaveChangesAsync();

        var servis = EkipServisiOlustur(dbContext);
        var yanit = await servis.KonumGuncelleAsync(ekip.Id, sahaPersoneli.Id, 41.0, 29.0);

        Assert.Equal(41.0, yanit.GuncelKonumEnlem);
        Assert.Equal(29.0, yanit.GuncelKonumBoylam);
    }

    [Fact]
    public async Task Ekip_KonumGuncelle_Uye_Olmayan_Personel_Icin_ErisimYetkisiYokException_Firlatir()
    {
        var (dbContext, birim) = BirimliOrtamHazirla();
        var ekip = birim.EkipEkle("Ekip-1");
        var uyeOlmayanPersonel = birim.PersonelEkle("Baska Personel", "baska.personel", "hash", "5550000003",
            PersonelRolu.SahaPersoneli);

        dbContext.Ekipler.Add(ekip);
        await dbContext.SaveChangesAsync();

        var servis = EkipServisiOlustur(dbContext);

        await Assert.ThrowsAsync<EkipErisimYetkisiYokException>(() =>
            servis.KonumGuncelleAsync(ekip.Id, uyeOlmayanPersonel.Id, 41.0, 29.0));
    }
}
