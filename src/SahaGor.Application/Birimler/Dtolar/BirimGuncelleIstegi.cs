using System.ComponentModel.DataAnnotations;

namespace SahaGor.Application.Birimler.Dtolar;

public sealed record BirimGuncelleIstegi(
    [property: Required, MaxLength(200)] string Ad,
    [property: MaxLength(1000)] string? Aciklama);
