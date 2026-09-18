namespace SahaGor.Application.Entegrasyonlar.Istisnalar;

/// <summary>Dis kaynaktan gelen basvurudaki kategori adi, sistemdeki hicbir aktif kategoriyle eslesmedi.</summary>
public sealed class GorevKategorisiAdiylaBulunamadiException : Exception
{
    public GorevKategorisiAdiylaBulunamadiException(string kategoriAdi)
        : base($"'{kategoriAdi}' adinda aktif bir gorev kategorisi bulunamadi.")
    {
    }
}
