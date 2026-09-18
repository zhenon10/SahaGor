namespace SahaGor.Application.Ekipler.Istisnalar;

public sealed class EkipBulunamadiException : Exception
{
    public EkipBulunamadiException(Guid id) : base($"'{id}' numarali ekip bulunamadi.")
    {
    }
}
