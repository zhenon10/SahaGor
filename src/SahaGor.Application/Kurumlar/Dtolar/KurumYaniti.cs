namespace SahaGor.Application.Kurumlar.Dtolar;

public sealed record KurumYaniti(
    Guid Id,
    string Ad,
    string? Adres,
    string? IletisimTelefonu,
    bool AktifMi,
    int BirimSayisi);
