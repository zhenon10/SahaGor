namespace SahaGor.Application.Gorevler.Dtolar;

/// <summary>Komuta paneli KPI dashboard'u icin ozet istatistikler (SG-321).</summary>
public sealed record GorevIstatistikleriYaniti(
    int AcikGorevSayisi,
    int SlaIhlalSayisi,
    double SlaIhlalOrani,
    double? OrtalamaCozumSuresiDakika,
    int PencereIcindeOlusturulanGorevSayisi,
    int PencereIcindeTamamlananGorevSayisi,
    int GunSayisi);
