namespace SahaGor.Application.Birimler.Istisnalar;

public sealed class BirimBulunamadiException : Exception
{
    public BirimBulunamadiException(Guid id) : base($"'{id}' numarali birim bulunamadi.")
    {
    }
}
