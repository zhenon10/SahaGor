namespace SahaGor.Application.Gorevler.Dtolar;

/// <summary>Detay ekraninda gosterilen, denetim izini ve fotograflari da iceren tam gorev bilgisi.</summary>
public sealed record GorevTalebiDetayYaniti(
    Guid Id,
    string Baslik,
    string? Aciklama,
    string KategoriAdi,
    string Durum,
    string Oncelik,
    string Kaynak,
    double Enlem,
    double Boylam,
    string? BolgeAdi,
    string? AtananEkipAdi,
    string? AtananPersonelAdi,
    string? BildirenTelefonNumarasi,
    DateTime OlusturulmaZamaniUtc,
    DateTime SlaHedefZamaniUtc,
    bool SlaIhlalEdildiMi,
    IReadOnlyList<GorevDurumGecmisiYaniti> DurumGecmisi,
    IReadOnlyList<GorevFotografiYaniti> Fotograflar);

public sealed record GorevDurumGecmisiYaniti(string OncekiDurum, string YeniDurum, DateTime DegisiklikZamaniUtc, string? Not);

public sealed record GorevFotografiYaniti(Guid Id, string DosyaYolu, string Asama, DateTime CekilmeZamaniUtc);
