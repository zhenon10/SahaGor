namespace SahaGor.Application.Kimlik.Istisnalar;

/// <summary>
/// Kullanici adi bulunamadi veya sifre yanlis oldugunda firlatilir. Bilincli olarak
/// hangi durumun gerceklestigini belirtmez; boylece saldirganin gecerli kullanici
/// adlarini deneme-yanilma (enumeration) ile tespit etmesi engellenir.
/// </summary>
public sealed class GecersizGirisException : Exception
{
    public GecersizGirisException() : base("Kullanıcı adı veya şifre hatalı.")
    {
    }
}
