using SahaGor.Application.Kimlik;

namespace SahaGor.Infrastructure.Kimlik;

/// <summary>BCrypt algoritmasi ile sifre hashleme (her hash'te otomatik farkli salt uretilir).</summary>
public sealed class BcryptSifreHashleyici : ISifreHashleyici
{
    // Work factor arttikca hash'leme suresi (ve saldirganin brute-force maliyeti) katlanarak artar.
    // 12, guncel donanimda ~250ms surer; kullanici deneyimini bozmadan guvenli bir denge saglar.
    private const int IsFaktoru = 12;

    public string Hashle(string duzMetinSifre)
    {
        if (string.IsNullOrWhiteSpace(duzMetinSifre))
        {
            throw new ArgumentException("Sifre bos olamaz.", nameof(duzMetinSifre));
        }

        return BCrypt.Net.BCrypt.HashPassword(duzMetinSifre, workFactor: IsFaktoru);
    }

    public bool Dogrula(string duzMetinSifre, string hash)
    {
        if (string.IsNullOrWhiteSpace(duzMetinSifre) || string.IsNullOrWhiteSpace(hash))
        {
            return false;
        }

        try
        {
            return BCrypt.Net.BCrypt.Verify(duzMetinSifre, hash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            // Veritabanindaki hash formati bozuksa (orn. elle girilmis duz metin), guvenli tarafta
            // kalip dogrulamayi basarisiz say; exception'i cagirana sizdirma.
            return false;
        }
    }
}
