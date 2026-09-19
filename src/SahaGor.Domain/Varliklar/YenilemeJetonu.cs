using SahaGor.Domain.Ortak;

namespace SahaGor.Domain.Varliklar;

/// <summary>
/// Bir personele ait yenileme (refresh) jetonu. Saha personelinin mobil uygulamada oturumunu
/// kapanmadan, offline senkronizasyon sirasinda dahi yenileyebilmesini saglar (SG-122).
/// Kullanildiktan sonra iptal edilir (rotasyon) — ayni jeton iki kez kullanilamaz.
/// </summary>
public class YenilemeJetonu : TemelVarlik
{
    public Guid PersonelId { get; private set; }

    public Personel? Personel { get; private set; }

    /// <summary>
    /// Jetonun kendisi DEGIL, SHA-256 hash'idir (OWASP A02 - Cryptographic Failures).
    /// Sifrelerde oldugu gibi: veritabani bir sekilde ele gecirilirse (yedek sizintisi, ic tehdit
    /// vb.) saldirganin dogrudan kullanilabilir jetonlara erismesini onler. Duz metin jeton
    /// SADECE bir kez, uretildigi anda istemciye donulur ve hicbir yerde saklanmaz.
    /// </summary>
    public string TokenHash { get; private set; } = null!;

    public DateTime SonKullanmaZamaniUtc { get; private set; }

    public bool IptalEdildiMi { get; private set; }

    public DateTime? IptalZamaniUtc { get; private set; }

    private YenilemeJetonu()
    {
    }

    public YenilemeJetonu(Guid personelId, string tokenHash, DateTime sonKullanmaZamaniUtc)
    {
        if (personelId == Guid.Empty)
        {
            throw new ArgumentException("Yenileme jetonu bir personele bagli olmalidir.", nameof(personelId));
        }

        if (string.IsNullOrWhiteSpace(tokenHash))
        {
            throw new ArgumentException("Jeton hash degeri bos olamaz.", nameof(tokenHash));
        }

        PersonelId = personelId;
        TokenHash = tokenHash;
        SonKullanmaZamaniUtc = sonKullanmaZamaniUtc;
    }

    public bool GecerliMi(DateTime? suAnUtc = null) =>
        !IptalEdildiMi && SonKullanmaZamaniUtc > (suAnUtc ?? DateTime.UtcNow);

    /// <summary>Jeton bir kez kullanildiginda veya iptal edildiginde tekrar kullanilamaz hale gelir.</summary>
    public void IptalEt()
    {
        if (IptalEdildiMi)
        {
            return;
        }

        IptalEdildiMi = true;
        IptalZamaniUtc = DateTime.UtcNow;
    }
}
