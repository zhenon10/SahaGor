using System.ComponentModel.DataAnnotations;

namespace SahaGor.Application.Kurumlar.Dtolar;

public sealed record KurumOlusturIstegi(
    [property: Required, MaxLength(200)] string Ad,
    [property: MaxLength(500)] string? Adres,
    [property: MaxLength(20)] string? IletisimTelefonu);
