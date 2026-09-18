using System.ComponentModel.DataAnnotations;

namespace SahaGor.Application.Ekipler.Dtolar;

public sealed record EkipGuncelleIstegi([Required, MaxLength(200)] string Ad);
