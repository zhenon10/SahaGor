using SahaGor.Application.Entegrasyonlar.Dtolar;
using SahaGor.Application.Gorevler.Dtolar;

namespace SahaGor.Application.Entegrasyonlar;

/// <summary>
/// 153/CIMER gibi dis sistemlerden gelen basvurulari GorevTalebi'ne cevirir (SG-401).
/// Kimlik dogrulama (HMAC imza kontrolu) bu servisin sorumlulugunda DEGILDIR; o, Api
/// katmanindaki webhook controller'inda, govde ham haldeyken yapilir (bkz. Hat153EntegrasyonController).
/// </summary>
public interface IDisKaynakBasvuruServisi
{
    Task<GorevTalebiDetayYaniti> Hat153BasvurusuIsleAsync(Hat153BasvuruIstegi istek, CancellationToken iptalToken = default);
}
