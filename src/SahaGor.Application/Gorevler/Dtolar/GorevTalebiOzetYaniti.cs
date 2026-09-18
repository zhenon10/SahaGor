namespace SahaGor.Application.Gorevler.Dtolar;

/// <summary>Liste ekraninda gosterilen ozet gorev bilgisi.</summary>
public sealed record GorevTalebiOzetYaniti(
    Guid Id,
    string Baslik,
    string KategoriAdi,
    string Durum,
    string Oncelik,
    double Enlem,
    double Boylam,
    string? BolgeAdi,
    string? AtananEkipAdi,
    DateTime OlusturulmaZamaniUtc,
    DateTime SlaHedefZamaniUtc,
    bool SlaIhlalEdildiMi);
