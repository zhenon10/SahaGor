namespace SahaGor.Application.Kimlik.Istisnalar;

/// <summary>Yenileme jetonu bulunamadi, suresi doldu, iptal edilmis veya hesap artik uygun degilse firlatilir.</summary>
public sealed class GecersizYenilemeJetonuException : Exception
{
    public GecersizYenilemeJetonuException() : base("Yenileme jetonu geçersiz veya süresi dolmuş. Lütfen tekrar giriş yapın.")
    {
    }
}
