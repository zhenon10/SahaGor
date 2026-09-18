namespace SahaGor.Application.Ekipler.Istisnalar;

/// <summary>Bir personel, uyesi olmadigi bir ekibin konumunu guncellemeye calistiginda firlatilir.</summary>
public sealed class EkipErisimYetkisiYokException : Exception
{
    public EkipErisimYetkisiYokException(Guid ekipId)
        : base($"'{ekipId}' numarali ekibin bir uyesi degilsiniz.")
    {
    }
}
