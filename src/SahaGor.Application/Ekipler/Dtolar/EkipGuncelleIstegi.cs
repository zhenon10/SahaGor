using System.ComponentModel.DataAnnotations;

namespace SahaGor.Application.Ekipler.Dtolar;

public sealed record EkipGuncelleIstegi([property: Required, MaxLength(200)] string Ad);
