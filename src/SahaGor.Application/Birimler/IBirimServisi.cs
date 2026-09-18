using SahaGor.Application.Birimler.Dtolar;

namespace SahaGor.Application.Birimler;

public interface IBirimServisi
{
    Task<BirimYaniti> OlusturAsync(BirimOlusturIstegi istek, CancellationToken iptalToken = default);

    Task<IReadOnlyList<BirimYaniti>> ListeleAsync(Guid? kurumId, CancellationToken iptalToken = default);

    Task<BirimYaniti> DetayGetirAsync(Guid id, CancellationToken iptalToken = default);

    Task<BirimYaniti> GuncelleAsync(Guid id, BirimGuncelleIstegi istek, CancellationToken iptalToken = default);

    Task DurumGuncelleAsync(Guid id, bool aktifMi, CancellationToken iptalToken = default);
}
