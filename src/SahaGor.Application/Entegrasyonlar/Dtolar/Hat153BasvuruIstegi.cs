namespace SahaGor.Application.Entegrasyonlar.Dtolar;

/// <summary>
/// 153 Belediye Iletisim Merkezi'nden (veya CIMER'den) webhook araciligiyla gelen basvuru
/// govdesi (SG-401). NOT: Gercek 153/CIMER entegrasyon semasi henuz netlesmedigi icin bu,
/// makul bir varsayimsal semadir; gercek dokumantasyon geldiginde alan eslemesi guncellenecektir.
/// </summary>
public sealed record Hat153BasvuruIstegi(
    string ReferansNo,
    string Baslik,
    string? Aciklama,
    double Enlem,
    double Boylam,
    string? BildirenTelefonu,
    string KategoriAdi);
