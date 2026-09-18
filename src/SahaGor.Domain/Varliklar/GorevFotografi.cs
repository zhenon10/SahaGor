using NetTopologySuite.Geometries;
using SahaGor.Domain.Enumlar;
using SahaGor.Domain.Ortak;

namespace SahaGor.Domain.Varliklar;

/// <summary>
/// Saha personelinin uygulama ici kamera ile cektigi, GPS konumu ve zaman damgasi ile
/// dogrulanmis kanit fotografi (SG-210, SG-211). Sadece GorevTalebi uzerinden olusturulur.
/// </summary>
public class GorevFotografi : TemelVarlik
{
    public Guid GorevTalebiId { get; private set; }

    public GorevTalebi? GorevTalebi { get; private set; }

    /// <summary>Dosya depolama servisindeki (orn. S3/Blob Storage) goreceli yol veya anahtar.</summary>
    public string DosyaYolu { get; private set; } = null!;

    public GorevFotografAsamasi Asama { get; private set; }

    /// <summary>Fotografin cekildigi andaki GPS konumu (SRID 4326).</summary>
    public Point CekildigiKonum { get; private set; } = null!;

    /// <summary>Cihazin bildirdigi cekim zamani (EXIF/uygulama ici damga).</summary>
    public DateTime CekilmeZamaniUtc { get; private set; }

    /// <summary>Sunucunun fotografi teslim aldigi zaman; sahtecilik tespiti icin CekilmeZamaniUtc ile karsilastirilir.</summary>
    public DateTime SunucuyaUlasmaZamaniUtc { get; private set; }

    public Guid CekenPersonelId { get; private set; }

    public Personel? CekenPersonel { get; private set; }

    private GorevFotografi()
    {
    }

    internal GorevFotografi(Guid gorevTalebiId, string dosyaYolu, GorevFotografAsamasi asama,
        Point cekildigiKonum, DateTime cekilmeZamaniUtc, Guid cekenPersonelId)
    {
        if (gorevTalebiId == Guid.Empty)
        {
            throw new ArgumentException("Fotograf bir goreve bagli olmalidir.", nameof(gorevTalebiId));
        }

        if (string.IsNullOrWhiteSpace(dosyaYolu))
        {
            throw new ArgumentException("Dosya yolu bos olamaz.", nameof(dosyaYolu));
        }

        if (cekenPersonelId == Guid.Empty)
        {
            throw new ArgumentException("Fotografi ceken personel belirtilmelidir.", nameof(cekenPersonelId));
        }

        var suAnUtc = DateTime.UtcNow;

        // Cihaz saatinin geriye/ileriye oynatilarak sahte kanit uretilmesini zorlastirmak icin
        // cekim zamaninin sunucu saatinden makul olcunun (5 dk) otesinde ileride olmasi reddedilir.
        if (cekilmeZamaniUtc > suAnUtc.AddMinutes(5))
        {
            throw new InvalidOperationException("Fotograf cekim zamani gelecekte olamaz; cihaz saati kontrol edilmeli.");
        }

        GorevTalebiId = gorevTalebiId;
        DosyaYolu = dosyaYolu;
        Asama = asama;
        CekildigiKonum = cekildigiKonum ?? throw new ArgumentNullException(nameof(cekildigiKonum));
        CekilmeZamaniUtc = cekilmeZamaniUtc;
        SunucuyaUlasmaZamaniUtc = suAnUtc;
        CekenPersonelId = cekenPersonelId;
    }
}
