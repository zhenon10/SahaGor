using SahaGor.Application.Personeller.Dtolar;

namespace SahaGor.Application.Personeller;

public interface IPersonelServisi
{
    Task<PersonelYaniti> OlusturAsync(PersonelOlusturIstegi istek, CancellationToken iptalToken = default);

    Task<IReadOnlyList<PersonelYaniti>> ListeleAsync(PersonelFiltre filtre, CancellationToken iptalToken = default);

    Task<PersonelYaniti> DetayGetirAsync(Guid id, CancellationToken iptalToken = default);

    Task<PersonelYaniti> GuncelleAsync(Guid id, PersonelGuncelleIstegi istek, CancellationToken iptalToken = default);

    Task DurumGuncelleAsync(Guid id, bool aktifMi, CancellationToken iptalToken = default);

    Task SifreSifirlaAsync(Guid id, SifreSifirlaIstegi istek, CancellationToken iptalToken = default);
}
