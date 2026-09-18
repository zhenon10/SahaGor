using NetTopologySuite.Geometries;
using SahaGor.Domain.Enumlar;
using SahaGor.Domain.Ortak;
using SahaGor.Domain.Varliklar;

namespace SahaGor.UnitTests.Varliklar;

public class GorevTalebiTests
{
    private static GorevKategorisi OrnekKategoriOlustur(int slaCozumDakika = 240) =>
        new("Kirik Kaldirim", slaYanitSuresiDakika: 30, slaCozumSuresiDakika: slaCozumDakika);

    private static Point OrnekKonumOlustur() => KonumFabrikasi.NoktaOlustur(enlem: 41.015137, boylam: 28.979530);

    private static GorevTalebi OrnekGorevOlustur() =>
        new(
            baslik: "Bagdat Caddesi kaldirim hasari",
            kategori: OrnekKategoriOlustur(),
            konum: OrnekKonumOlustur(),
            oncelik: GorevOnceligi.Normal,
            kaynak: GorevKaynagi.OperatorGirisi);

    [Fact]
    public void Yeni_gorev_Acildi_durumunda_baslar_ve_denetim_izine_kaydedilir()
    {
        var gorev = OrnekGorevOlustur();

        Assert.Equal(GorevDurumu.Acildi, gorev.Durum);
        Assert.Single(gorev.DurumGecmisi);
    }

    [Fact]
    public void SlaHedefZamani_kategori_cozum_suresine_gore_hesaplanir()
    {
        var gorev = OrnekGorevOlustur();

        var beklenenHedef = gorev.OlusturulmaZamaniUtc.AddMinutes(240);

        Assert.Equal(beklenenHedef, gorev.SlaHedefZamaniUtc);
    }

    [Fact]
    public void Gecerli_durum_akisi_basindan_sonuna_basariyla_ilerler()
    {
        var gorev = OrnekGorevOlustur();
        var birim = new Birim(Guid.NewGuid(), "Fen Isleri Mudurlugu");
        var ekip = birim.EkipEkle("Ekip-1");
        var amirId = Guid.NewGuid();
        var personel = birim.PersonelEkle("Ahmet Yilmaz", "ahmet.yilmaz", "hash", "5551112233",
            SahaGor.Domain.Enumlar.PersonelRolu.SahaPersoneli);
        ekip.UyeEkle(personel);

        gorev.EkibeAta(ekip, amirId, personel);
        gorev.YolaCik(personel.Id);
        gorev.Baslat(personel.Id);
        gorev.FotografEkle("fotograflar/kanit-1.jpg", GorevFotografAsamasi.Sonra, OrnekKonumOlustur(),
            DateTime.UtcNow, personel.Id);
        gorev.Tamamla(personel.Id);
        gorev.Dogrula(amirId);

        Assert.Equal(GorevDurumu.Dogrulandi, gorev.Durum);
        Assert.Equal(6, gorev.DurumGecmisi.Count); // Acildi + Atandi + YolaCikildi + Baslandi + Tamamlandi + Dogrulandi
    }

    [Fact]
    public void Fotografsiz_gorev_tamamlanamaz()
    {
        var gorev = OrnekGorevOlustur();
        var birim = new Birim(Guid.NewGuid(), "Fen Isleri Mudurlugu");
        var ekip = birim.EkipEkle("Ekip-1");
        var personel = birim.PersonelEkle("Ahmet Yilmaz", "ahmet.yilmaz", "hash", "5551112233",
            SahaGor.Domain.Enumlar.PersonelRolu.SahaPersoneli);
        ekip.UyeEkle(personel);

        gorev.EkibeAta(ekip, Guid.NewGuid(), personel);
        gorev.YolaCik(personel.Id);
        gorev.Baslat(personel.Id);

        Assert.Throws<InvalidOperationException>(() => gorev.Tamamla(personel.Id));
    }

    [Fact]
    public void Dogrulanmis_gorev_tekrar_hicbir_duruma_gecemez()
    {
        var gorev = OrnekGorevOlustur();
        var birim = new Birim(Guid.NewGuid(), "Fen Isleri Mudurlugu");
        var ekip = birim.EkipEkle("Ekip-1");
        var personel = birim.PersonelEkle("Ahmet Yilmaz", "ahmet.yilmaz", "hash", "5551112233",
            SahaGor.Domain.Enumlar.PersonelRolu.SahaPersoneli);
        ekip.UyeEkle(personel);

        gorev.EkibeAta(ekip, Guid.NewGuid(), personel);
        gorev.YolaCik(personel.Id);
        gorev.Baslat(personel.Id);
        gorev.FotografEkle("fotograflar/kanit-1.jpg", GorevFotografAsamasi.Sonra, OrnekKonumOlustur(),
            DateTime.UtcNow, personel.Id);
        gorev.Tamamla(personel.Id);
        gorev.Dogrula(Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(() => gorev.IptalEt(Guid.NewGuid(), "gecersiz deneme"));
    }

    [Fact]
    public void SlaHedefi_gecmis_ve_hala_acik_gorev_ihlal_olarak_isaretlenir()
    {
        var gorev = OrnekGorevOlustur();

        var kontrolZamani = gorev.SlaHedefZamaniUtc.AddMinutes(1);

        Assert.True(gorev.SlaIhlalEdildiMi(kontrolZamani));
    }

    [Fact]
    public void Bolge_sinirlari_disindaki_konuma_bolge_atanamaz()
    {
        var gorev = OrnekGorevOlustur();

        var uzakBolgePolygon = KonumFabrikasi.NoktaOlustur(10, 10).Buffer(0.001) as Polygon;
        var bolge = new Bolge(Guid.NewGuid(), "Ilgisiz Bolge", uzakBolgePolygon!);

        Assert.Throws<InvalidOperationException>(() => gorev.BolgeAta(bolge));
    }
}
