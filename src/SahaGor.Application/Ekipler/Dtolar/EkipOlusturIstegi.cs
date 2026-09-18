using System.ComponentModel.DataAnnotations;

namespace SahaGor.Application.Ekipler.Dtolar;

public sealed record EkipOlusturIstegi(
    [property: Required] Guid BirimId,
    [property: Required, MaxLength(200)] string Ad);
