using SahaGor.Domain.Varliklar;

namespace SahaGor.Application.Kimlik;

/// <summary>Erisim (JWT) ve yenileme jetonu uretimini soyutlar.</summary>
public interface IJwtTokenUretici
{
    (string Token, DateTime SonKullanmaZamaniUtc) ErisimTokeniUret(Personel personel);

    /// <summary>Kriptografik olarak guvenli, tahmin edilemez rastgele bir yenileme jetonu degeri uretir.</summary>
    string YenilemeTokeniUret();

    DateTime YenilemeTokeniSonKullanmaZamaniHesapla();
}
