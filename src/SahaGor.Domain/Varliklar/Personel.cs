using SahaGor.Domain.Enumlar;
using SahaGor.Domain.Ortak;

namespace SahaGor.Domain.Varliklar;

/// <summary>
/// Kurum bunyesinde calisan, sisteme giris yapabilen kullanici (operator, saha personeli, amir, sistem yoneticisi).
/// </summary>
public class Personel : TemelVarlik
{
    public Guid BirimId { get; private set; }

    public Birim? Birim { get; private set; }

    public Guid? EkipId { get; private set; }

    public Ekip? Ekip { get; private set; }

    public string AdSoyad { get; private set; } = null!;

    public string KullaniciAdi { get; private set; } = null!;

    /// <summary>Sifrenin kendisi degil, guvenli bir algoritma (orn. BCrypt) ile uretilmis hash degeri tutulur.</summary>
    public string SifreHash { get; private set; } = null!;

    public string Telefon { get; private set; } = null!;

    public string? Eposta { get; private set; }

    public PersonelRolu Rol { get; private set; }

    public bool AktifMi { get; private set; } = true;

    /// <summary>Otomatik atama algoritmasinin (Sprint 3) yeni gorev onerip onermeyecegini belirler.</summary>
    public bool MusaitMi { get; private set; } = true;

    /// <summary>Art arda yanlis sifre denemesi sayisi. Basarili girişte sifirlanir.</summary>
    public int BasarisizGirisSayisi { get; private set; }

    /// <summary>Doluysa ve gelecekte bir zamansa, hesap bu zamana kadar kilitli demektir.</summary>
    public DateTime? KilitlenmeBitisZamaniUtc { get; private set; }

    /// <summary>Bu sayida ust uste yanlis sifre denemesinden sonra hesap kilitlenir (SG-120 kabul kriteri).</summary>
    private const int KilitlenmeIcinGerekliBasarisizDenemeSayisi = 5;

    /// <summary>Hesabin kilitli kalacagi sure.</summary>
    private static readonly TimeSpan KilitlenmeSuresi = TimeSpan.FromMinutes(15);

    private Personel()
    {
    }

    public Personel(Guid birimId, string adSoyad, string kullaniciAdi, string sifreHash, string telefon,
        PersonelRolu rol, string? eposta = null)
    {
        if (birimId == Guid.Empty)
        {
            throw new ArgumentException("Personel bir birime bagli olmalidir.", nameof(birimId));
        }

        if (string.IsNullOrWhiteSpace(adSoyad))
        {
            throw new ArgumentException("Ad soyad bos olamaz.", nameof(adSoyad));
        }

        if (string.IsNullOrWhiteSpace(kullaniciAdi))
        {
            throw new ArgumentException("Kullanici adi bos olamaz.", nameof(kullaniciAdi));
        }

        if (string.IsNullOrWhiteSpace(sifreHash))
        {
            throw new ArgumentException("Sifre hash bos olamaz.", nameof(sifreHash));
        }

        if (string.IsNullOrWhiteSpace(telefon))
        {
            throw new ArgumentException("Telefon numarasi bos olamaz.", nameof(telefon));
        }

        BirimId = birimId;
        AdSoyad = adSoyad;
        KullaniciAdi = kullaniciAdi;
        SifreHash = sifreHash;
        Telefon = telefon;
        Eposta = eposta;
        Rol = rol;
    }

    public void EkibeAta(Ekip ekip)
    {
        ArgumentNullException.ThrowIfNull(ekip);

        if (Rol != PersonelRolu.SahaPersoneli)
        {
            throw new InvalidOperationException("Sadece saha personeli bir ekibe atanabilir.");
        }

        EkipId = ekip.Id;
        Ekip = ekip;
    }

    public void EkiptenCikar()
    {
        EkipId = null;
        Ekip = null;
    }

    public void MusaitlikDurumunuGuncelle(bool musaitMi) => MusaitMi = musaitMi;

    public void TemelBilgileriGuncelle(string adSoyad, string telefon, string? eposta)
    {
        if (string.IsNullOrWhiteSpace(adSoyad))
        {
            throw new ArgumentException("Ad soyad bos olamaz.", nameof(adSoyad));
        }

        if (string.IsNullOrWhiteSpace(telefon))
        {
            throw new ArgumentException("Telefon numarasi bos olamaz.", nameof(telefon));
        }

        AdSoyad = adSoyad;
        Telefon = telefon;
        Eposta = eposta;
    }

    public void SifreyiGuncelle(string yeniSifreHash)
    {
        if (string.IsNullOrWhiteSpace(yeniSifreHash))
        {
            throw new ArgumentException("Sifre hash bos olamaz.", nameof(yeniSifreHash));
        }

        SifreHash = yeniSifreHash;
    }

    public void Pasiflestir() => AktifMi = false;

    public void Aktiflestir() => AktifMi = true;

    /// <summary>Hesabin su an icin (brute-force korumasi nedeniyle) kilitli olup olmadigini kontrol eder.</summary>
    public bool KilitliMi(DateTime? suAnUtc = null) =>
        KilitlenmeBitisZamaniUtc.HasValue && KilitlenmeBitisZamaniUtc.Value > (suAnUtc ?? DateTime.UtcNow);

    /// <summary>Yanlis sifre girisinde cagrilir; esik asilirsa hesabi gecici olarak kilitler.</summary>
    public void BasarisizGirisKaydet()
    {
        BasarisizGirisSayisi++;

        if (BasarisizGirisSayisi >= KilitlenmeIcinGerekliBasarisizDenemeSayisi)
        {
            KilitlenmeBitisZamaniUtc = DateTime.UtcNow.Add(KilitlenmeSuresi);
        }
    }

    /// <summary>Basarili girişte deneme sayacini ve varsa kilidi sifirlar.</summary>
    public void BasariliGirisKaydet()
    {
        BasarisizGirisSayisi = 0;
        KilitlenmeBitisZamaniUtc = null;
    }
}
