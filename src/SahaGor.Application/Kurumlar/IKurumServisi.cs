using SahaGor.Application.Kurumlar.Dtolar;

namespace SahaGor.Application.Kurumlar;

public interface IKurumServisi
{
    Task<KurumYaniti> OlusturAsync(KurumOlusturIstegi istek, CancellationToken iptalToken = default);

    Task<IReadOnlyList<KurumYaniti>> ListeleAsync(CancellationToken iptalToken = default);

    Task<KurumYaniti> DetayGetirAsync(Guid id, CancellationToken iptalToken = default);

    Task<KurumYaniti> GuncelleAsync(Guid id, KurumGuncelleIstegi istek, CancellationToken iptalToken = default);

    Task DurumGuncelleAsync(Guid id, bool aktifMi, CancellationToken iptalToken = default);
}
