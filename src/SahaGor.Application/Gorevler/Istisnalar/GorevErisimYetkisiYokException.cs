namespace SahaGor.Application.Gorevler.Istisnalar;

/// <summary>Personel, kendi ekibine atanmamis bir gorev uzerinde islem yapmaya calistiginda firlatilir.</summary>
public sealed class GorevErisimYetkisiYokException : Exception
{
    public GorevErisimYetkisiYokException(Guid gorevId)
        : base($"'{gorevId}' numarali gorev sizin ekibinize atanmamis.")
    {
    }
}
