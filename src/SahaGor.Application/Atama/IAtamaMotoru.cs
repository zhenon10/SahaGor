using SahaGor.Application.Atama.Dtolar;

namespace SahaGor.Application.Atama;

/// <summary>
/// Bir goreve en uygun ekibi agirlikli skorlama ile oneren motorun sozlesmesi (SG-310).
/// Agirliklar: mesafe %40, musaitlik %30, yetkinlik %20, is yuku %10. Motor HICBIR ZAMAN
/// otomatik atama YAPMAZ; sadece oneri uretir - nihai onay her zaman Amir'e aittir (SG-311).
/// </summary>
public interface IAtamaMotoru
{
    Task<AtamaOnerisiYaniti> OneriHesaplaAsync(Guid gorevId, CancellationToken iptalToken = default);
}
