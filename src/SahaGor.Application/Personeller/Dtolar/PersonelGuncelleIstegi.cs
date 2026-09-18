using System.ComponentModel.DataAnnotations;

namespace SahaGor.Application.Personeller.Dtolar;

public sealed record PersonelGuncelleIstegi(
    [Required, MaxLength(200)] string AdSoyad,
    [Required, MaxLength(20)] string Telefon,
    [MaxLength(200)] string? Eposta);
