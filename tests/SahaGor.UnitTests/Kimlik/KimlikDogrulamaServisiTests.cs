using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SahaGor.Application.Kimlik.Dtolar;
using SahaGor.Application.Kimlik.Istisnalar;
using SahaGor.Domain.Enumlar;
using SahaGor.Domain.Varliklar;
using SahaGor.Infrastructure.Kimlik;
using SahaGor.Infrastructure.Persistence;

namespace SahaGor.UnitTests.Kimlik;

public class KimlikDogrulamaServisiTests
{
    private const string DogruSifre = "GucluSifre!2026";
    private const string JwtTestAnahtari = "test-ortami-icin-en-az-32-karakter-uzunlugunda-anahtar";

    private static SahaGorDbContext InBellekDbContextOlustur() =>
        new(new DbContextOptionsBuilder<SahaGorDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static JwtTokenUretici JwtTokenUreticiOlustur() =>
        new(Options.Create(new JwtAyarlari { Anahtar = JwtTestAnahtari, ErisimTokeniDakika = 15, YenilemeTokeniGunSayisi = 7 }));

    private static (SahaGorDbContext DbContext, KimlikDogrulamaServisi Servis, Personel Personel) OrtamHazirla()
    {
        var dbContext = InBellekDbContextOlustur();
        var sifreHashleyici = new BcryptSifreHashleyici();
        var jwtTokenUretici = JwtTokenUreticiOlustur();

        var birim = new Birim(Guid.NewGuid(), "Fen Isleri Mudurlugu");
        var personel = birim.PersonelEkle("Ahmet Yilmaz", "ahmet.yilmaz", sifreHashleyici.Hashle(DogruSifre),
            "5551112233", PersonelRolu.Amir);

        dbContext.Birimler.Add(birim);
        dbContext.SaveChanges();

        var servis = new KimlikDogrulamaServisi(dbContext, sifreHashleyici, jwtTokenUretici,
            NullLogger<KimlikDogrulamaServisi>.Instance);

        return (dbContext, servis, personel);
    }

    [Fact]
    public async Task Dogru_bilgilerle_giris_erisim_ve_yenileme_tokeni_doner()
    {
        var (_, servis, personel) = OrtamHazirla();

        var yanit = await servis.GirisYapAsync(new GirisIstegi(personel.KullaniciAdi, DogruSifre));

        Assert.False(string.IsNullOrWhiteSpace(yanit.ErisimTokeni));
        Assert.False(string.IsNullOrWhiteSpace(yanit.YenilemeTokeni));
        Assert.Equal(personel.Id, yanit.PersonelId);
        Assert.Equal(PersonelRolu.Amir.ToString(), yanit.Rol);
    }

    [Fact]
    public async Task Yanlis_sifre_ile_giris_GecersizGirisException_firlatir()
    {
        var (_, servis, personel) = OrtamHazirla();

        await Assert.ThrowsAsync<GecersizGirisException>(() =>
            servis.GirisYapAsync(new GirisIstegi(personel.KullaniciAdi, "yanlis-sifre")));
    }

    [Fact]
    public async Task Tanimsiz_kullanici_adiyla_giris_GecersizGirisException_firlatir()
    {
        var (_, servis, _) = OrtamHazirla();

        await Assert.ThrowsAsync<GecersizGirisException>(() =>
            servis.GirisYapAsync(new GirisIstegi("olmayan.kullanici", DogruSifre)));
    }

    [Fact]
    public async Task Bes_basarisiz_denemeden_sonra_hesap_kilitlenir()
    {
        var (dbContext, servis, personel) = OrtamHazirla();

        for (var i = 0; i < 5; i++)
        {
            await Assert.ThrowsAsync<GecersizGirisException>(() =>
                servis.GirisYapAsync(new GirisIstegi(personel.KullaniciAdi, "yanlis-sifre")));
        }

        // Sifre artik dogru girilse bile hesap kilitli oldugu icin giris reddedilmeli.
        await Assert.ThrowsAsync<HesapKilitliException>(() =>
            servis.GirisYapAsync(new GirisIstegi(personel.KullaniciAdi, DogruSifre)));

        var guncelPersonel = await dbContext.Personeller.SingleAsync(p => p.Id == personel.Id);
        Assert.True(guncelPersonel.KilitliMi());
    }

    [Fact]
    public async Task Yenileme_tokeni_ile_yeni_jeton_cifti_alinabilir_ve_eski_jeton_tekrar_kullanilamaz()
    {
        var (_, servis, personel) = OrtamHazirla();

        var ilkGiris = await servis.GirisYapAsync(new GirisIstegi(personel.KullaniciAdi, DogruSifre));
        var yenilenmis = await servis.TokenYenileAsync(new YenilemeIstegi(ilkGiris.YenilemeTokeni));

        Assert.NotEqual(ilkGiris.ErisimTokeni, yenilenmis.ErisimTokeni);
        Assert.NotEqual(ilkGiris.YenilemeTokeni, yenilenmis.YenilemeTokeni);

        // Rotasyon nedeniyle ayni yenileme tokeni ikinci kez kullanilamamalidir.
        await Assert.ThrowsAsync<GecersizYenilemeJetonuException>(() =>
            servis.TokenYenileAsync(new YenilemeIstegi(ilkGiris.YenilemeTokeni)));
    }

    [Fact]
    public async Task Gecersiz_yenileme_tokeni_ile_istek_reddedilir()
    {
        var (_, servis, _) = OrtamHazirla();

        await Assert.ThrowsAsync<GecersizYenilemeJetonuException>(() =>
            servis.TokenYenileAsync(new YenilemeIstegi("olmayan-bir-token-degeri")));
    }

    [Fact]
    public async Task Yenileme_tokeni_veritabaninda_duz_metin_olarak_DEGIL_hash_olarak_saklanir()
    {
        // OWASP A02 (Cryptographic Failures): veritabani bir sekilde ele gecirilirse
        // (yedek sizintisi, ic tehdit vb.) saldirganin dogrudan kullanilabilir yenileme
        // jetonlarina erismemesi gerekir - tipki sifreler gibi.
        var (dbContext, servis, personel) = OrtamHazirla();

        var giris = await servis.GirisYapAsync(new GirisIstegi(personel.KullaniciAdi, DogruSifre));

        var kayitliJeton = await dbContext.YenilemeJetonlari.SingleAsync(j => j.PersonelId == personel.Id);

        Assert.NotEqual(giris.YenilemeTokeni, kayitliJeton.TokenHash);
        Assert.DoesNotContain(giris.YenilemeTokeni, kayitliJeton.TokenHash);
    }
}
