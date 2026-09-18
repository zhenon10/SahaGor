using SahaGor.Application.Kimlik.Dtolar;

namespace SahaGor.Application.Kimlik;

/// <summary>
/// Giris ve token yenileme akislarinin is kurallarini (kilitlenme, sifre dogrulama,
/// jeton rotasyonu) yoneten servis sozlesmesi. Api katmani sadece bu arayuzu bilir.
/// </summary>
public interface IKimlikDogrulamaServisi
{
    Task<GirisYaniti> GirisYapAsync(GirisIstegi istek, CancellationToken iptalToken = default);

    Task<GirisYaniti> TokenYenileAsync(YenilemeIstegi istek, CancellationToken iptalToken = default);
}
