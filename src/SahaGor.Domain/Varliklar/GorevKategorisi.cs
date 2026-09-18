using SahaGor.Domain.Ortak;

namespace SahaGor.Domain.Varliklar;

/// <summary>
/// Gorev turu (orn. "Kirik Kaldirim", "Sokak Lambasi Arizasi") ve buna bagli SLA sureleri.
/// SLA alarm sistemi (Sprint 4) bu degerleri temel alir.
/// </summary>
public class GorevKategorisi : TemelVarlik
{
    public string Ad { get; private set; } = null!;

    public string? Aciklama { get; private set; }

    /// <summary>Gorevin ilk yanit (ekibe atanma) icin hedeflenen azami sure (dakika).</summary>
    public int SlaYanitSuresiDakika { get; private set; }

    /// <summary>Gorevin tamamen cozulmesi icin hedeflenen azami sure (dakika).</summary>
    public int SlaCozumSuresiDakika { get; private set; }

    public bool AktifMi { get; private set; } = true;

    private GorevKategorisi()
    {
    }

    public GorevKategorisi(string ad, int slaYanitSuresiDakika, int slaCozumSuresiDakika, string? aciklama = null)
    {
        if (string.IsNullOrWhiteSpace(ad))
        {
            throw new ArgumentException("Kategori adi bos olamaz.", nameof(ad));
        }

        if (slaYanitSuresiDakika <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(slaYanitSuresiDakika), slaYanitSuresiDakika,
                "SLA yanit suresi pozitif olmalidir.");
        }

        if (slaCozumSuresiDakika <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(slaCozumSuresiDakika), slaCozumSuresiDakika,
                "SLA cozum suresi pozitif olmalidir.");
        }

        if (slaCozumSuresiDakika < slaYanitSuresiDakika)
        {
            throw new ArgumentException("SLA cozum suresi, yanit suresinden kisa olamaz.", nameof(slaCozumSuresiDakika));
        }

        Ad = ad;
        Aciklama = aciklama;
        SlaYanitSuresiDakika = slaYanitSuresiDakika;
        SlaCozumSuresiDakika = slaCozumSuresiDakika;
    }

    public void SlaSurelerimiGuncelle(int slaYanitSuresiDakika, int slaCozumSuresiDakika)
    {
        if (slaYanitSuresiDakika <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(slaYanitSuresiDakika), slaYanitSuresiDakika,
                "SLA yanit suresi pozitif olmalidir.");
        }

        if (slaCozumSuresiDakika < slaYanitSuresiDakika)
        {
            throw new ArgumentException("SLA cozum suresi, yanit suresinden kisa olamaz.", nameof(slaCozumSuresiDakika));
        }

        SlaYanitSuresiDakika = slaYanitSuresiDakika;
        SlaCozumSuresiDakika = slaCozumSuresiDakika;
    }

    public void Pasiflestir() => AktifMi = false;

    public void Aktiflestir() => AktifMi = true;
}
