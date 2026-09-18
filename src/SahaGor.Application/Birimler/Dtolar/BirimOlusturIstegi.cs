using System.ComponentModel.DataAnnotations;

namespace SahaGor.Application.Birimler.Dtolar;

public sealed record BirimOlusturIstegi(
    [Required] Guid KurumId,
    [Required, MaxLength(200)] string Ad,
    [MaxLength(1000)] string? Aciklama);
