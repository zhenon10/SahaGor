using NetTopologySuite.Geometries;
using SahaGor.Domain.Enumlar;
using SahaGor.Domain.Ortak;

namespace SahaGor.Domain.Varliklar;

/// <summary>
/// SahaGor platformunun merkezi agregat koku: vatandas sikayeti, 153/CIMER kaydi veya operator
/// girisiyle olusan bir is emri. Durum gecisleri, SLA hesaplamasi ve denetim izi bu sinif
/// icinde tutarlilik saglanarak yonetilir.
/// </summary>
public class GorevTalebi : TemelVarlik
{
    private readonly List<GorevFotografi> _fotograflar = new();
    private readonly List<GorevDurumGecmisi> _durumGecmisi = new();

    /// <summary>
    /// Durum makinesi: anahtar mevcut durumu, deger o durumdan gecis yapilabilecek durumlari belirtir.
    /// Gecersiz bir gecis denendiginde InvalidOperationException firlatilir.
    /// </summary>
    private static readonly Dictionary<GorevDurumu, GorevDurumu[]> GecerliGecisler = new()
    {
        [GorevDurumu.Acildi] = new[] { GorevDurumu.Atandi, GorevDurumu.Atanamadi, GorevDurumu.Iptal },
        [GorevDurumu.Atanamadi] = new[] { GorevDurumu.Atandi, GorevDurumu.Iptal },
        [GorevDurumu.Atandi] = new[] { GorevDurumu.YolaCikildi, GorevDurumu.Iptal },
        [GorevDurumu.YolaCikildi] = new[] { GorevDurumu.Baslandi, GorevDurumu.Iptal },
        [GorevDurumu.Baslandi] = new[] { GorevDurumu.Tamamlandi, GorevDurumu.Iptal },
        [GorevDurumu.Tamamlandi] = new[] { GorevDurumu.Dogrulandi },
        [GorevDurumu.Dogrulandi] = Array.Empty<GorevDurumu>(),
        [GorevDurumu.Iptal] = Array.Empty<GorevDurumu>(),
    };

    public string Baslik { get; private set; } = null!;

    public string? Aciklama { get; private set; }

    public Guid KategoriId { get; private set; }

    public GorevKategorisi? Kategori { get; private set; }

    /// <summary>Gorevin coğrafi konumu (SRID 4326).</summary>
    public Point Konum { get; private set; } = null!;

    public Guid? BolgeId { get; private set; }

    public Bolge? Bolge { get; private set; }

    public GorevDurumu Durum { get; private set; }

    public GorevOnceligi Oncelik { get; private set; }

    public GorevKaynagi Kaynak { get; private set; }

    /// <summary>153/CIMER gibi dis sistemlerdeki orijinal basvuru numarasi.</summary>
    public string? DisKaynakReferansNo { get; private set; }

    /// <summary>Tamamlanma SMS bildirimi (SG-402) icin vatandasin telefon numarasi.</summary>
    public string? BildirenTelefonNumarasi { get; private set; }

    public Guid? AtananEkipId { get; private set; }

    public Ekip? AtananEkip { get; private set; }

    public Guid? AtananPersonelId { get; private set; }

    public Personel? AtananPersonel { get; private set; }

    public DateTime OlusturulmaZamaniUtc { get; private set; }

    /// <summary>Kategorinin SLA cozum suresine gore hesaplanan hedef tamamlanma zamani.</summary>
    public DateTime SlaHedefZamaniUtc { get; private set; }

    public DateTime? AtanmaZamaniUtc { get; private set; }

    public DateTime? YolaCikmaZamaniUtc { get; private set; }

    public DateTime? BaslamaZamaniUtc { get; private set; }

    public DateTime? TamamlanmaZamaniUtc { get; private set; }

    public DateTime? DogrulanmaZamaniUtc { get; private set; }

    public DateTime? IptalZamaniUtc { get; private set; }

    public IReadOnlyCollection<GorevFotografi> Fotograflar => _fotograflar.AsReadOnly();

    public IReadOnlyCollection<GorevDurumGecmisi> DurumGecmisi => _durumGecmisi.AsReadOnly();

    private GorevTalebi()
    {
    }

    public GorevTalebi(
        string baslik,
        GorevKategorisi kategori,
        Point konum,
        GorevOnceligi oncelik,
        GorevKaynagi kaynak,
        string? aciklama = null,
        string? bildirenTelefonNumarasi = null,
        string? disKaynakReferansNo = null)
    {
        if (string.IsNullOrWhiteSpace(baslik))
        {
            throw new ArgumentException("Gorev basligi bos olamaz.", nameof(baslik));
        }

        ArgumentNullException.ThrowIfNull(kategori);
        ArgumentNullException.ThrowIfNull(konum);

        Baslik = baslik;
        Aciklama = aciklama;
        KategoriId = kategori.Id;
        Kategori = kategori;
        Konum = konum;
        Oncelik = oncelik;
        Kaynak = kaynak;
        BildirenTelefonNumarasi = bildirenTelefonNumarasi;
        DisKaynakReferansNo = disKaynakReferansNo;

        Durum = GorevDurumu.Acildi;
        OlusturulmaZamaniUtc = DateTime.UtcNow;
        SlaHedefZamaniUtc = OlusturulmaZamaniUtc.AddMinutes(kategori.SlaCozumSuresiDakika);

        // Ilk kayit bir "gecis" degil, olusum anidir; bu yuzden durum makinesi kontrolunden
        // gecirmeden dogrudan denetim izine (audit trail) eklenir.
        _durumGecmisi.Add(new GorevDurumGecmisi(Id, GorevDurumu.Acildi, GorevDurumu.Acildi,
            islemiYapanPersonelId: null, not: "Gorev olusturuldu."));
    }

    /// <summary>Konumu, sinirlarini kapsayan bolgeye otomatik olarak baglar (SG-115, PostGIS ST_Contains).</summary>
    public void BolgeAta(Bolge bolge)
    {
        ArgumentNullException.ThrowIfNull(bolge);

        if (!bolge.NoktayiKapsiyorMu(Konum))
        {
            throw new InvalidOperationException("Gorev konumu, atanmak istenen bolgenin sinirlari disinda.");
        }

        BolgeId = bolge.Id;
        Bolge = bolge;
    }

    public void EkibeAta(Ekip ekip, Guid atamayiYapanPersonelId, Personel? sorumluPersonel = null)
    {
        ArgumentNullException.ThrowIfNull(ekip);
        DurumGecisiniDogrula(GorevDurumu.Atandi);

        AtananEkipId = ekip.Id;
        AtananEkip = ekip;
        AtananPersonelId = sorumluPersonel?.Id;
        AtananPersonel = sorumluPersonel;
        AtanmaZamaniUtc = DateTime.UtcNow;

        DurumGecisiniUygula(GorevDurumu.Atandi, atamayiYapanPersonelId, $"{ekip.Ad} ekibine atandi.");
    }

    /// <summary>Otomatik atama algoritmasi uygun ekip bulamadiginda cagrilir (Sprint 3).</summary>
    public void AtanamadiOlarakIsaretle(string neden)
    {
        DurumGecisiniDogrula(GorevDurumu.Atanamadi);
        DurumGecisiniUygula(GorevDurumu.Atanamadi, islemiYapanPersonelId: null, neden);
    }

    public void YolaCik(Guid personelId)
    {
        DurumGecisiniDogrula(GorevDurumu.YolaCikildi);
        YolaCikmaZamaniUtc = DateTime.UtcNow;
        DurumGecisiniUygula(GorevDurumu.YolaCikildi, personelId, "Personel yola cikti.");
    }

    public void Baslat(Guid personelId)
    {
        DurumGecisiniDogrula(GorevDurumu.Baslandi);
        BaslamaZamaniUtc = DateTime.UtcNow;
        DurumGecisiniUygula(GorevDurumu.Baslandi, personelId, "Gorev islemine baslandi.");
    }

    public void Tamamla(Guid personelId)
    {
        DurumGecisiniDogrula(GorevDurumu.Tamamlandi);

        if (_fotograflar.Count == 0)
        {
            throw new InvalidOperationException("Kanit fotografi eklenmeden gorev tamamlanamaz.");
        }

        TamamlanmaZamaniUtc = DateTime.UtcNow;
        DurumGecisiniUygula(GorevDurumu.Tamamlandi, personelId, "Gorev sahada tamamlandi.");
    }

    public void Dogrula(Guid dogrulayanPersonelId)
    {
        DurumGecisiniDogrula(GorevDurumu.Dogrulandi);
        DogrulanmaZamaniUtc = DateTime.UtcNow;
        DurumGecisiniUygula(GorevDurumu.Dogrulandi, dogrulayanPersonelId, "Amir tarafindan dogrulandi.");
    }

    /// <summary>Amir, kanitlari yetersiz bulursa tamamlanan gorevi sahaya geri gonderebilir.</summary>
    public void SahayaGeriGonder(Guid amirId, string neden)
    {
        if (Durum != GorevDurumu.Tamamlandi)
        {
            throw new InvalidOperationException("Sadece tamamlanmis bir gorev sahaya geri gonderilebilir.");
        }

        if (string.IsNullOrWhiteSpace(neden))
        {
            throw new ArgumentException("Geri gonderme nedeni belirtilmelidir.", nameof(neden));
        }

        TamamlanmaZamaniUtc = null;
        Durum = GorevDurumu.Baslandi;
        _durumGecmisi.Add(new GorevDurumGecmisi(Id, GorevDurumu.Tamamlandi, GorevDurumu.Baslandi,
            amirId, $"Amir tarafindan sahaya geri gonderildi: {neden}"));
    }

    public void IptalEt(Guid islemiYapanPersonelId, string neden)
    {
        if (Durum is GorevDurumu.Tamamlandi or GorevDurumu.Dogrulandi)
        {
            throw new InvalidOperationException("Tamamlanmis veya dogrulanmis bir gorev iptal edilemez.");
        }

        if (string.IsNullOrWhiteSpace(neden))
        {
            throw new ArgumentException("Iptal nedeni belirtilmelidir.", nameof(neden));
        }

        IptalZamaniUtc = DateTime.UtcNow;
        DurumGecisiniUygula(GorevDurumu.Iptal, islemiYapanPersonelId, neden);
    }

    public GorevFotografi FotografEkle(string dosyaYolu, GorevFotografAsamasi asama, Point cekildigiKonum,
        DateTime cekilmeZamaniUtc, Guid cekenPersonelId)
    {
        var fotograf = new GorevFotografi(Id, dosyaYolu, asama, cekildigiKonum, cekilmeZamaniUtc, cekenPersonelId);
        _fotograflar.Add(fotograf);
        return fotograf;
    }

    /// <summary>SLA hedefi asilmis mi? Kapanmis (Tamamlandi/Dogrulandi) gorevler icin ihlal sayilmaz.</summary>
    public bool SlaIhlalEdildiMi(DateTime? suAnUtc = null)
    {
        var kontrolZamani = suAnUtc ?? DateTime.UtcNow;
        var kapanmis = Durum is GorevDurumu.Tamamlandi or GorevDurumu.Dogrulandi;
        return !kapanmis && kontrolZamani > SlaHedefZamaniUtc;
    }

    /// <summary>SLA suresinin yuzde kaci tuketildigini hesaplar (Sprint 4 alarm esikleri icin, 0-100 arasi).</summary>
    public double SlaTuketimYuzdesi(DateTime? suAnUtc = null)
    {
        var simdi = suAnUtc ?? DateTime.UtcNow;
        var toplamSureDakika = (SlaHedefZamaniUtc - OlusturulmaZamaniUtc).TotalMinutes;

        if (toplamSureDakika <= 0)
        {
            return 100;
        }

        var gecenSureDakika = (simdi - OlusturulmaZamaniUtc).TotalMinutes;
        return Math.Clamp(gecenSureDakika / toplamSureDakika * 100, 0, 100);
    }

    private void DurumGecisiniDogrula(GorevDurumu hedefDurum)
    {
        if (!GecerliGecisler.TryGetValue(Durum, out var izinVerilenler) || !izinVerilenler.Contains(hedefDurum))
        {
            throw new InvalidOperationException(
                $"Gorev '{Durum}' durumundan '{hedefDurum}' durumuna gecemez.");
        }
    }

    private void DurumGecisiniUygula(GorevDurumu yeniDurum, Guid? islemiYapanPersonelId, string not)
    {
        var oncekiDurum = Durum;
        Durum = yeniDurum;
        _durumGecmisi.Add(new GorevDurumGecmisi(Id, oncekiDurum, yeniDurum, islemiYapanPersonelId, not));
    }
}
