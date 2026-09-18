namespace SahaGor.Infrastructure.Kimlik;

/// <summary>appsettings.json / ortam degiskenlerinden baglanan JWT yapilandirma degerleri.</summary>
public sealed class JwtAyarlari
{
    /// <summary>appsettings.json icindeki bolum adi: "Jwt".</summary>
    public const string BolumAdi = "Jwt";

    /// <summary>Token imzalama anahtari. En az 32 karakter (256 bit) olmalidir.</summary>
    public string Anahtar { get; set; } = string.Empty;

    public string Yayinlayan { get; set; } = "SahaGor";

    public string Kitle { get; set; } = "SahaGor.Istemciler";

    public int ErisimTokeniDakika { get; set; } = 15;

    public int YenilemeTokeniGunSayisi { get; set; } = 7;

    /// <summary>Yapilandirmanin guvenli sekilde kullanilabilir olup olmadigini dogrular (fail-fast).</summary>
    public void Dogrula()
    {
        if (string.IsNullOrWhiteSpace(Anahtar) || Anahtar.Length < 32)
        {
            throw new InvalidOperationException(
                "Jwt:Anahtar yapilandirmasi eksik veya yetersiz uzunlukta (en az 32 karakter olmalidir). " +
                "Uretim ortaminda bu deger bir ortam degiskeni/secret store uzerinden saglanmalidir.");
        }
    }
}
