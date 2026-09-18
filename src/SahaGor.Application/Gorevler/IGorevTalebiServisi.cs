using SahaGor.Application.Gorevler.Dtolar;
using SahaGor.Domain.Enumlar;

namespace SahaGor.Application.Gorevler;

/// <summary>
/// GorevTalebi CRUD ve coğrafi sorgu is akislarinin sozlesmesi (SG-130, SG-131).
/// Api katmani sadece bu arayuzu bilir; PostGIS/EF Core detaylari Infrastructure'da kalir.
/// </summary>
public interface IGorevTalebiServisi
{
    Task<GorevTalebiDetayYaniti> OlusturAsync(GorevTalebiOlusturIstegi istek, Guid? olusturanPersonelId,
        CancellationToken iptalToken = default);

    Task<SayfalanmisSonuc<GorevTalebiOzetYaniti>> ListeleAsync(GorevTalebiFiltre filtre,
        CancellationToken iptalToken = default);

    Task<GorevTalebiDetayYaniti> DetayGetirAsync(Guid id, CancellationToken iptalToken = default);

    /// <summary>Verilen merkez noktaya "yaricapMetre" mesafesindeki acik gorevleri, mesafeye gore artan sirada dondurur.</summary>
    Task<IReadOnlyList<YakinimdakiGorevYaniti>> YakinimdakiGetirAsync(double enlem, double boylam,
        double yaricapMetre, int maksimumSonucSayisi, CancellationToken iptalToken = default);

    Task IptalEtAsync(Guid id, string neden, Guid iptalEdenPersonelId, CancellationToken iptalToken = default);

    /// <summary>
    /// Amir'in bir gorevi bir ekibe atamasi (SG-311). Hem atama motorunun onerisini "tek
    /// tikla onaylamak" hem de tamamen manuel bir ekip secmek icin AYNI uc nokta kullanilir.
    /// </summary>
    Task<GorevTalebiDetayYaniti> AtaAsync(Guid gorevId, Guid ekipId, Guid? sorumluPersonelId, Guid atayanPersonelId,
        CancellationToken iptalToken = default);

    /// <summary>Saha personeli gorev konumuna dogru yola ciktiginda cagrilir (SG-220, SG-202/203 senkronu icin hedef uc nokta).</summary>
    Task<GorevTalebiDetayYaniti> YolaCikAsync(Guid gorevId, Guid personelId, CancellationToken iptalToken = default);

    /// <summary>Saha personeli sahada islemeye basladiginda cagrilir.</summary>
    Task<GorevTalebiDetayYaniti> BaslatAsync(Guid gorevId, Guid personelId, CancellationToken iptalToken = default);

    /// <summary>Komuta paneli KPI dashboard'u icin ozet istatistikler (SG-321).</summary>
    Task<GorevIstatistikleriYaniti> IstatistikleriGetirAsync(int gunSayisi, CancellationToken iptalToken = default);

    /// <summary>
    /// Amir, sahada tamamlanan bir isi kanitlarla birlikte inceleyip onaylar (SG-311'in
    /// tamamlanma tarafi). Vatandasa SMS bildirimi (SG-402) bu adimda tetiklenir; "Tamamlandi"
    /// asamasinda degil, cunku is Amir tarafindan sahaya geri gonderilebilir (yanlis bildirim riski).
    /// </summary>
    Task<GorevTalebiDetayYaniti> DogrulaAsync(Guid gorevId, Guid dogrulayanPersonelId, CancellationToken iptalToken = default);

    /// <summary>Amir, kanitlari yetersiz bulursa tamamlanan isi sahaya geri gonderir.</summary>
    Task<GorevTalebiDetayYaniti> SahayaGeriGonderAsync(Guid gorevId, Guid amirId, string neden,
        CancellationToken iptalToken = default);

    /// <summary>
    /// Offline cekilip bekletilen bir kanit fotografini, baglanti geldiginde sunucuya yukler (SG-212).
    /// Konum ve cekim zamani, mobil cihazda cekim aninda kaydedilmis degerlerdir (SG-211).
    /// </summary>
    Task<GorevFotografiYaniti> FotografEkleAsync(Guid gorevId, Guid personelId, Stream fotografIcerigi,
        string dosyaUzantisi, GorevFotografAsamasi asama, double enlem, double boylam, DateTime cekilmeZamaniUtc,
        CancellationToken iptalToken = default);
}
