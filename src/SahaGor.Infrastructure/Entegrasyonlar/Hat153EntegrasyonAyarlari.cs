namespace SahaGor.Infrastructure.Entegrasyonlar;

/// <summary>appsettings.json / ortam degiskenlerinden baglanan 153 webhook yapilandirmasi.</summary>
public sealed class Hat153EntegrasyonAyarlari
{
    public const string BolumAdi = "Entegrasyonlar:Hat153";

    /// <summary>153 sistemiyle paylasilan, HMAC-SHA256 imza dogrulamasinda kullanilan gizli anahtar.</summary>
    public string PaylasilanGizliAnahtar { get; set; } = string.Empty;

    public void Dogrula()
    {
        if (string.IsNullOrWhiteSpace(PaylasilanGizliAnahtar) || PaylasilanGizliAnahtar.Length < 16)
        {
            throw new InvalidOperationException(
                "Entegrasyonlar:Hat153:PaylasilanGizliAnahtar tanimli degil veya cok kisa (en az 16 karakter). " +
                "153 webhook'u guvenli sekilde dogrulanamadan calismamalidir.");
        }
    }
}
