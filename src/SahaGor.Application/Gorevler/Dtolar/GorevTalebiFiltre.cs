using SahaGor.Domain.Enumlar;

namespace SahaGor.Application.Gorevler.Dtolar;

/// <summary>GET /api/gorevler icin destekelenen filtre ve sayfalama parametreleri (SG-130).</summary>
public sealed class GorevTalebiFiltre
{
    public GorevDurumu? Durum { get; init; }

    public Guid? KategoriId { get; init; }

    public Guid? BolgeId { get; init; }

    /// <summary>Mobil uygulamanin "atanmis gorevlerim" listesi icin: sadece bu ekibe atanmis gorevler.</summary>
    public Guid? AtananEkipId { get; init; }

    public Guid? AtananPersonelId { get; init; }

    public DateTime? BaslangicTarihiUtc { get; init; }

    public DateTime? BitisTarihiUtc { get; init; }

    private const int VarsayilanSayfaBoyutu = 20;
    private const int AzamiSayfaBoyutu = 100;

    private int _sayfa = 1;
    public int Sayfa
    {
        get => _sayfa;
        init => _sayfa = value < 1 ? 1 : value;
    }

    private int _sayfaBoyutu = VarsayilanSayfaBoyutu;
    public int SayfaBoyutu
    {
        get => _sayfaBoyutu;
        init => _sayfaBoyutu = value is < 1 or > AzamiSayfaBoyutu ? VarsayilanSayfaBoyutu : value;
    }
}
