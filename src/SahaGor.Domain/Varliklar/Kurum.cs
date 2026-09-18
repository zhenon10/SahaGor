using SahaGor.Domain.Ortak;

namespace SahaGor.Domain.Varliklar;

/// <summary>
/// SahaGor platformunu kullanan belediye/kurum. Hiyerarsinin en tepesindeki agregat koku.
/// </summary>
public class Kurum : TemelVarlik
{
    private readonly List<Birim> _birimler = new();

    public string Ad { get; private set; } = null!;

    public string? Adres { get; private set; }

    public string? IletisimTelefonu { get; private set; }

    public bool AktifMi { get; private set; } = true;

    public IReadOnlyCollection<Birim> Birimler => _birimler.AsReadOnly();

    /// <summary>EF Core'un materyalizasyon sirasinda kullandigi parametresiz kurucu.</summary>
    private Kurum()
    {
    }

    public Kurum(string ad, string? adres = null, string? iletisimTelefonu = null)
    {
        if (string.IsNullOrWhiteSpace(ad))
        {
            throw new ArgumentException("Kurum adi bos olamaz.", nameof(ad));
        }

        Ad = ad;
        Adres = adres;
        IletisimTelefonu = iletisimTelefonu;
    }

    public Birim BirimEkle(string ad, string? aciklama = null)
    {
        var birim = new Birim(Id, ad, aciklama);
        _birimler.Add(birim);
        return birim;
    }

    public void BilgileriGuncelle(string ad, string? adres, string? iletisimTelefonu)
    {
        if (string.IsNullOrWhiteSpace(ad))
        {
            throw new ArgumentException("Kurum adi bos olamaz.", nameof(ad));
        }

        Ad = ad;
        Adres = adres;
        IletisimTelefonu = iletisimTelefonu;
    }

    public void Pasiflestir() => AktifMi = false;

    public void Aktiflestir() => AktifMi = true;
}
