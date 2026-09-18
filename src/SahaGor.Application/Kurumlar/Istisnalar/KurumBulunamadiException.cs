namespace SahaGor.Application.Kurumlar.Istisnalar;

public sealed class KurumBulunamadiException : Exception
{
    public KurumBulunamadiException(Guid id) : base($"'{id}' numarali kurum bulunamadi.")
    {
    }
}
