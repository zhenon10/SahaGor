using System.ComponentModel.DataAnnotations;
using SahaGor.Domain.Enumlar;

namespace SahaGor.Application.Personeller.Dtolar;

public sealed record PersonelOlusturIstegi(
    [property: Required] Guid BirimId,
    [property: Required, MaxLength(200)] string AdSoyad,
    [property: Required, MaxLength(100)] string KullaniciAdi,
    [property: Required, MinLength(8), MaxLength(100)] string Sifre,
    [property: Required, MaxLength(20)] string Telefon,
    [property: Required] PersonelRolu Rol,
    [property: MaxLength(200)] string? Eposta);
