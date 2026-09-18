namespace SahaGor.Domain.Ortak;

/// <summary>
/// Tum Domain varliklarinin turedigi temel sinif. Kimlik, denetim izi (audit trail)
/// ve yumusak silme (soft delete) davranislarini merkezi olarak saglar (SG-133).
/// </summary>
public abstract class TemelVarlik
{
    public Guid Id { get; private set; }

    public DateTime OlusturmaZamaniUtc { get; private set; }

    public Guid? OlusturanKullaniciId { get; private set; }

    public DateTime? GuncellemeZamaniUtc { get; private set; }

    public Guid? GuncelleyenKullaniciId { get; private set; }

    public bool SilindiMi { get; private set; }

    public DateTime? SilinmeZamaniUtc { get; private set; }

    protected TemelVarlik()
    {
        Id = Guid.NewGuid();
        OlusturmaZamaniUtc = DateTime.UtcNow;
    }

    public void OlusturanKullaniciyiAyarla(Guid kullaniciId)
    {
        // Sadece varlik ilk olusturuldugunda bir kez atanir; sonradan degistirilmesi
        // denetim izinin (audit trail) guvenilirligini bozar.
        OlusturanKullaniciId ??= kullaniciId;
    }

    public void GuncellendiOlarakIsaretle(Guid guncelleyenKullaniciId)
    {
        GuncellemeZamaniUtc = DateTime.UtcNow;
        GuncelleyenKullaniciId = guncelleyenKullaniciId;
    }

    public void YumusakSil(Guid silenKullaniciId)
    {
        if (SilindiMi)
        {
            return;
        }

        SilindiMi = true;
        SilinmeZamaniUtc = DateTime.UtcNow;
        GuncellendiOlarakIsaretle(silenKullaniciId);
    }

    public void SilmeyiGeriAl()
    {
        SilindiMi = false;
        SilinmeZamaniUtc = null;
    }
}
