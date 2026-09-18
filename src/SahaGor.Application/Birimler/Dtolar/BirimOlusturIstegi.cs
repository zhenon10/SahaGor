using System.ComponentModel.DataAnnotations;

namespace SahaGor.Application.Birimler.Dtolar;

public sealed record BirimOlusturIstegi(
    [property: Required] Guid KurumId,
    [property: Required, MaxLength(200)] string Ad,
    [property: MaxLength(1000)] string? Aciklama);
