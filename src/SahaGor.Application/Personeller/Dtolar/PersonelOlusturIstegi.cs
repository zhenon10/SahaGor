using System.ComponentModel.DataAnnotations;
using SahaGor.Domain.Enumlar;

namespace SahaGor.Application.Personeller.Dtolar;

public sealed record PersonelOlusturIstegi(
    [Required] Guid BirimId,
    [Required, MaxLength(200)] string AdSoyad,
    [Required, MaxLength(100)] string KullaniciAdi,
    [Required, MinLength(8), MaxLength(100)] string Sifre,
    [Required, MaxLength(20)] string Telefon,
    [Required] PersonelRolu Rol,
    [MaxLength(200)] string? Eposta);
