namespace SahaGor.Application.Birimler.Dtolar;

public sealed record BirimYaniti(
    Guid Id,
    Guid KurumId,
    string KurumAdi,
    string Ad,
    string? Aciklama,
    bool AktifMi,
    int PersonelSayisi,
    int EkipSayisi);
