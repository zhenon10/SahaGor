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

    public string Token { get; private set; } = null!;

    public DateTime SonKullanmaZamaniUtc { get; private set; }

    public bool IptalEdildiMi { get; private set; }

    public DateTime? IptalZamaniUtc { get; private set; }

    private YenilemeJetonu()
    {
    }

    public YenilemeJetonu(Guid personelId, string token, DateTime sonKullanmaZamaniUtc)
    {
        if (personelId == Guid.Empty)
        {
            throw new ArgumentException("Yenileme jetonu bir personele bagli olmalidir.", nameof(personelId));
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ArgumentException("Jeton degeri bos olamaz.", nameof(token));
        }

        PersonelId = personelId;
        Token = token;
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
