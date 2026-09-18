namespace SahaGor.Application.Gorevler.Istisnalar;

public sealed class GorevKategorisiPasifException : Exception
{
    public GorevKategorisiPasifException(string kategoriAdi)
        : base($"'{kategoriAdi}' kategorisi pasif durumda; yeni gorev acilamaz.")
    {
    }
}
