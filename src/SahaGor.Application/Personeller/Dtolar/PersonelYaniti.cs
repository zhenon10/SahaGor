namespace SahaGor.Application.Personeller.Dtolar;

public sealed record PersonelYaniti(
    Guid Id,
    Guid BirimId,
    string BirimAdi,
    Guid? EkipId,
    string? EkipAdi,
    string AdSoyad,
    string KullaniciAdi,
    string Telefon,
    string? Eposta,
    string Rol,
    bool AktifMi,
    bool MusaitMi,
    bool KilitliMi);
