namespace SahaGor.Application.Gorevler.Istisnalar;

public sealed class GorevTalebiBulunamadiException : Exception
{
    public GorevTalebiBulunamadiException(Guid id) : base($"'{id}' numarali gorev talebi bulunamadi.")
    {
    }
}
