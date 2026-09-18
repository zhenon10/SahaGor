namespace SahaGor.Application.Kimlik.Dtolar;

/// <summary>Basarili giris/yenileme sonrasi donen erisim ve yenileme jetonu ciftini tasir.</summary>
public sealed record GirisYaniti(
    string ErisimTokeni,
    DateTime ErisimTokeniSonKullanmaZamaniUtc,
    string YenilemeTokeni,
    DateTime YenilemeTokeniSonKullanmaZamaniUtc,
    Guid PersonelId,
    string AdSoyad,
    string Rol);
