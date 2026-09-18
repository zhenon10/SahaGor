namespace SahaGor.Application.Gorevler.Istisnalar;

public sealed class GorevKategorisiBulunamadiException : Exception
{
    public GorevKategorisiBulunamadiException(Guid kategoriId)
        : base($"'{kategoriId}' numarali gorev kategorisi bulunamadi.")
    {
    }
}
