using NetTopologySuite.Geometries;
using SahaGor.Domain.Enumlar;
using SahaGor.Domain.Ortak;

namespace SahaGor.Domain.Varliklar;

/// <summary>
/// Sahada gorev yapan personelden olusan ekip. Canli konumu ve uzmanlik alanlari,
/// Sprint 3'teki otomatik atama motoru ve canli harita icin temel veriyi olusturur.
/// </summary>
public class Ekip : TemelVarlik
{
    private readonly List<Personel> _uyeler = new();
    private readonly List<GorevKategorisi> _uzmanlikAlanlari = new();

    public Guid BirimId { get; private set; }

    public Birim? Birim { get; private set; }

    public string Ad { get; private set; } = null!;

    /// <summary>Ekibin en son bildirilen coğrafi konumu (SRID 4326). Canli haritada pin olarak gosterilir.</summary>
    public Point? GuncelKonum { get; private set; }

    public DateTime? KonumGuncellenmeZamaniUtc { get; private set; }

    public bool AktifMi { get; private set; } = true;

    public IReadOnlyCollection<Personel> Uyeler => _uyeler.AsReadOnly();

    public IReadOnlyCollection<GorevKategorisi> UzmanlikAlanlari => _uzmanlikAlanlari.AsReadOnly();

    private Ekip()
    {
    }

    public Ekip(Guid birimId, string ad)
    {
        if (birimId == Guid.Empty)
        {
            throw new ArgumentException("Ekip bir birime bagli olmalidir.", nameof(birimId));
        }

        if (string.IsNullOrWhiteSpace(ad))
        {
            throw new ArgumentException("Ekip adi bos olamaz.", nameof(ad));
        }

        BirimId = birimId;
        Ad = ad;
    }

    /// <summary>Mobil uygulamadan gelen GPS konum bildirimini isler (SG-301, SG-302 icin temel veri).</summary>
    public void KonumGuncelle(double enlem, double boylam)
    {
        GuncelKonum = KonumFabrikasi.NoktaOlustur(enlem, boylam);
        KonumGuncellenmeZamaniUtc = DateTime.UtcNow;
    }

    public void UyeEkle(Personel personel)
    {
        ArgumentNullException.ThrowIfNull(personel);

        if (personel.Rol != PersonelRolu.SahaPersoneli)
        {
            throw new InvalidOperationException("Ekibe sadece saha personeli rolundeki calisanlar eklenebilir.");
        }

        if (_uyeler.Any(u => u.Id == personel.Id))
        {
            return;
        }

        _uyeler.Add(personel);
        personel.EkibeAta(this);
    }

    public void UyeCikar(Personel personel)
    {
        ArgumentNullException.ThrowIfNull(personel);

        if (_uyeler.RemoveAll(u => u.Id == personel.Id) > 0)
        {
            personel.EkiptenCikar();
        }
    }

    public void UzmanlikAlaniEkle(GorevKategorisi kategori)
    {
        ArgumentNullException.ThrowIfNull(kategori);

        if (_uzmanlikAlanlari.Any(k => k.Id == kategori.Id))
        {
            return;
        }

        _uzmanlikAlanlari.Add(kategori);
    }

    public void UzmanlikAlaniCikar(Guid gorevKategorisiId) => _uzmanlikAlanlari.RemoveAll(k => k.Id == gorevKategorisiId);

    public bool UzmanMi(Guid gorevKategorisiId) => _uzmanlikAlanlari.Any(k => k.Id == gorevKategorisiId);

    public void AdiGuncelle(string ad)
    {
        if (string.IsNullOrWhiteSpace(ad))
        {
            throw new ArgumentException("Ekip adi bos olamaz.", nameof(ad));
        }

        Ad = ad;
    }

    /// <summary>Ekibin o an ustlenebilecegi kadar bosta oldugunu, uye musaitligine bakarak belirler.</summary>
    public bool MusaitMi() => AktifMi && _uyeler.Count > 0 && _uyeler.Any(u => u.AktifMi && u.MusaitMi);

    public void Pasiflestir() => AktifMi = false;

    public void Aktiflestir() => AktifMi = true;
}
