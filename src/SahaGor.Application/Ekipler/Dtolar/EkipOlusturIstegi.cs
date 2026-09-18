using System.ComponentModel.DataAnnotations;

namespace SahaGor.Application.Ekipler.Dtolar;

public sealed record EkipOlusturIstegi(
    [Required] Guid BirimId,
    [Required, MaxLength(200)] string Ad);
