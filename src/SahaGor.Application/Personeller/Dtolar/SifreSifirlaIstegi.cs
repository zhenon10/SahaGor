using System.ComponentModel.DataAnnotations;

namespace SahaGor.Application.Personeller.Dtolar;

/// <summary>Amir/SistemYoneticisi tarafindan bir personelin sifresini idari olarak sifirlamak icin.</summary>
public sealed record SifreSifirlaIstegi([property: Required, MinLength(8), MaxLength(100)] string YeniSifre);
