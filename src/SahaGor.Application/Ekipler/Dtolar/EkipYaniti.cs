namespace SahaGor.Application.Ekipler.Dtolar;

public sealed record EkipYaniti(
    Guid Id,
    Guid BirimId,
    string BirimAdi,
    string Ad,
    bool AktifMi,
    double? GuncelKonumEnlem,
    double? GuncelKonumBoylam,
    DateTime? KonumGuncellenmeZamaniUtc,
    IReadOnlyList<EkipUyesiYaniti> Uyeler,
    IReadOnlyList<EkipUzmanlikAlaniYaniti> UzmanlikAlanlari);

public sealed record EkipUyesiYaniti(Guid PersonelId, string AdSoyad, bool AktifMi, bool MusaitMi);

public sealed record EkipUzmanlikAlaniYaniti(Guid GorevKategorisiId, string Ad);
