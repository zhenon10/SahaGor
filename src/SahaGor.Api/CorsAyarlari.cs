namespace SahaGor.Api;

/// <summary>
/// appsettings.json'dan okunan CORS yapilandirmasi (SG-421'de gercek bir tarayiciyla E2E test
/// yazilirken ortaya cikan production hatasini duzeltir): web admin paneli (Next.js) ile
/// backend API farkli origin'lerde (farkli port dahi olsa) calistigindan, tarayicidan
/// dogrudan yapilan (Authorization header'li) cagrilar CORS on-kontrolu (preflight) olmadan
/// tarayici tarafindan engellenir.
/// </summary>
public sealed class CorsAyarlari
{
    public const string BolumAdi = "Cors";

    /// <summary>Web admin panelinin calistigi, tarayicidan API'ye erisimine izin verilen origin'ler.</summary>
    public string[] IzinVerilenKaynaklar { get; set; } = Array.Empty<string>();

    public void Dogrula()
    {
        if (IzinVerilenKaynaklar.Length == 0)
        {
            throw new InvalidOperationException(
                "'Cors:IzinVerilenKaynaklar' en az bir adres icermelidir (orn. web admin panelinin " +
                "genel adresi). Aksi halde tarayicidan yapilan TUM kimlik dogrulamali API cagrilari " +
                "(CORS on-kontrolu basarisiz oldugu icin) sessizce engellenir.");
        }
    }
}
