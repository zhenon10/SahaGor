namespace SahaGor.Application.Personeller.Istisnalar;

public sealed class KullaniciAdiZatenKullanimdaException : Exception
{
    public KullaniciAdiZatenKullanimdaException(string kullaniciAdi)
        : base($"'{kullaniciAdi}' kullanici adi zaten kullanimda.")
    {
    }
}
