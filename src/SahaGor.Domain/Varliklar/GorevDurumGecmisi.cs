using SahaGor.Domain.Enumlar;
using SahaGor.Domain.Ortak;

namespace SahaGor.Domain.Varliklar;

/// <summary>
/// Bir GorevTalebi'nin durum degisikliklerinin degistirilemez (immutable) denetim izi (audit trail).
/// Sadece GorevTalebi agregat koku tarafindan olusturulur (SG-112).
/// </summary>
public class GorevDurumGecmisi : TemelVarlik
{
    public Guid GorevTalebiId { get; private set; }

    public GorevTalebi? GorevTalebi { get; private set; }

    public GorevDurumu OncekiDurum { get; private set; }

    public GorevDurumu YeniDurum { get; private set; }

    /// <summary>Islemi yapan personel; sistem tarafindan otomatik degisikliklerde null olabilir.</summary>
    public Guid? IslemiYapanPersonelId { get; private set; }

    public string? Not { get; private set; }

    public DateTime DegisiklikZamaniUtc { get; private set; }

    private GorevDurumGecmisi()
    {
    }

    internal GorevDurumGecmisi(Guid gorevTalebiId, GorevDurumu oncekiDurum, GorevDurumu yeniDurum,
        Guid? islemiYapanPersonelId, string? not)
    {
        if (gorevTalebiId == Guid.Empty)
        {
            throw new ArgumentException("Durum gecmisi bir goreve bagli olmalidir.", nameof(gorevTalebiId));
        }

        GorevTalebiId = gorevTalebiId;
        OncekiDurum = oncekiDurum;
        YeniDurum = yeniDurum;
        IslemiYapanPersonelId = islemiYapanPersonelId;
        Not = not;
        DegisiklikZamaniUtc = DateTime.UtcNow;
    }
}
