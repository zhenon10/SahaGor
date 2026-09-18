using SahaGor.Application.Ekipler.Dtolar;

namespace SahaGor.Application.Ekipler;

public interface IEkipServisi
{
    Task<EkipYaniti> OlusturAsync(EkipOlusturIstegi istek, CancellationToken iptalToken = default);

    Task<IReadOnlyList<EkipYaniti>> ListeleAsync(Guid? birimId, CancellationToken iptalToken = default);

    Task<EkipYaniti> DetayGetirAsync(Guid id, CancellationToken iptalToken = default);

    Task<EkipYaniti> GuncelleAsync(Guid id, EkipGuncelleIstegi istek, CancellationToken iptalToken = default);

    Task DurumGuncelleAsync(Guid id, bool aktifMi, CancellationToken iptalToken = default);

    Task<EkipYaniti> UyeEkleAsync(Guid ekipId, Guid personelId, CancellationToken iptalToken = default);

    Task<EkipYaniti> UyeCikarAsync(Guid ekipId, Guid personelId, CancellationToken iptalToken = default);

    Task<EkipYaniti> UzmanlikAlaniEkleAsync(Guid ekipId, Guid gorevKategorisiId, CancellationToken iptalToken = default);

    Task<EkipYaniti> UzmanlikAlaniCikarAsync(Guid ekipId, Guid gorevKategorisiId, CancellationToken iptalToken = default);

    /// <summary>
    /// Mobil uygulamadan gelen GPS bildirimiyle ekibin canli konumunu gunceller (SG-301, SG-302).
    /// Sadece ekip uyesi olan personel kendi ekibinin konumunu guncelleyebilir.
    /// </summary>
    Task<EkipYaniti> KonumGuncelleAsync(Guid ekipId, Guid personelId, double enlem, double boylam,
        CancellationToken iptalToken = default);
}
