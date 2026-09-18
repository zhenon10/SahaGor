using System.ComponentModel.DataAnnotations;

namespace SahaGor.Application.Kurumlar.Dtolar;

public sealed record KurumOlusturIstegi(
    [Required, MaxLength(200)] string Ad,
    [MaxLength(500)] string? Adres,
    [MaxLength(20)] string? IletisimTelefonu);
