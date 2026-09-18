using SahaGor.Domain.Enumlar;

namespace SahaGor.Application.Personeller.Dtolar;

public sealed class PersonelFiltre
{
    public Guid? BirimId { get; init; }

    public PersonelRolu? Rol { get; init; }

    public bool? AktifMi { get; init; }
}
