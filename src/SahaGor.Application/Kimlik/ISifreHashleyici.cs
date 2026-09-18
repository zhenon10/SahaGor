namespace SahaGor.Application.Kimlik;

/// <summary>
/// Sifre hashleme/dogrulama islemini soyutlar. Application katmani hangi algoritmanin
/// (BCrypt, Argon2 vb.) kullanildigini bilmez; bu bir Infrastructure detayidir.
/// </summary>
public interface ISifreHashleyici
{
    string Hashle(string duzMetinSifre);

    bool Dogrula(string duzMetinSifre, string hash);
}
