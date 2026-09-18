using SahaGor.Domain.Ortak;

namespace SahaGor.Domain.Varliklar;

/// <summary>
/// Kurum bunyesindeki bir birim/mudurluk (orn. Fen Isleri Mudurlugu, Temizlik Isleri Mudurlugu).
/// Personel ve ekipler bir birime baglidir.
/// </summary>
public class Birim : TemelVarlik
{
    private readonly List<Personel> _personeller = new();
    private readonly List<Ekip> _ekipler = new();

    public Guid KurumId { get; private set; }

    public Kurum? Kurum { get; private set; }

    public string Ad { get; private set; } = null!;

    public string? Aciklama { get; private set; }

    public bool AktifMi { get; private set; } = true;

    public IReadOnlyCollection<Personel> Personeller => _personeller.AsReadOnly();

    public IReadOnlyCollection<Ekip> Ekipler => _ekipler.AsReadOnly();

    private Birim()
    {
    }

    public Birim(Guid kurumId, string ad, string? aciklama = null)
    {
        if (kurumId == Guid.Empty)
        {
            throw new ArgumentException("Birim bir kuruma bagli olmalidir.", nameof(kurumId));
        }

        if (string.IsNullOrWhiteSpace(ad))
        {
            throw new ArgumentException("Birim adi bos olamaz.", nameof(ad));
        }

        KurumId = kurumId;
        Ad = ad;
        Aciklama = aciklama;
    }

    public Personel PersonelEkle(string adSoyad, string kullaniciAdi, string sifreHash, string telefon,
        Enumlar.PersonelRolu rol, string? eposta = null)
    {
        var personel = new Personel(Id, adSoyad, kullaniciAdi, sifreHash, telefon, rol, eposta);
        _personeller.Add(personel);
        return personel;
    }

    public Ekip EkipEkle(string ad)
    {
        var ekip = new Ekip(Id, ad);
        _ekipler.Add(ekip);
        return ekip;
    }

    public void BilgileriGuncelle(string ad, string? aciklama)
    {
        if (string.IsNullOrWhiteSpace(ad))
        {
            throw new ArgumentException("Birim adi bos olamaz.", nameof(ad));
        }

        Ad = ad;
        Aciklama = aciklama;
    }

    public void Pasiflestir() => AktifMi = false;

    public void Aktiflestir() => AktifMi = true;
}
