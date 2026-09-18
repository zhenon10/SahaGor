using System.ComponentModel.DataAnnotations;

namespace SahaGor.Application.Personeller.Dtolar;

public sealed record PersonelGuncelleIstegi(
    [property: Required, MaxLength(200)] string AdSoyad,
    [property: Required, MaxLength(20)] string Telefon,
    [property: MaxLength(200)] string? Eposta);
