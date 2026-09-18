namespace SahaGor.Application.Atama.Dtolar;

/// <summary>
/// Bir ekibin atama algoritmasindaki agirlikli skoru ve bu skoru olusturan bilesenler
/// (SG-312 - kararin aciklanabilir olmasi icin her bilesen ayri ayri saklanir/donulur).
/// </summary>
public sealed record AtamaAdayiYaniti(
    Guid EkipId,
    string EkipAdi,
    double ToplamSkor,
    double MesafeSkoru,
    double MusaitlikSkoru,
    double YetkinlikSkoru,
    double IsYukuSkoru,
    double? MesafeMetre,
    int AktifGorevSayisi);
