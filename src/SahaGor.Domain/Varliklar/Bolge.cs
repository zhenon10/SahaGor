using NetTopologySuite.Geometries;
using SahaGor.Domain.Ortak;

namespace SahaGor.Domain.Varliklar;

/// <summary>
/// Kurumun sorumluluk alanindaki bir mahalle/bolge siniri. Gelen gorevler, konumlarina gore
/// bu polygon'lar ile eslestirilerek otomatik bolgelendirilir (SG-115).
/// </summary>
public class Bolge : TemelVarlik
{
    public Guid KurumId { get; private set; }

    public Kurum? Kurum { get; private set; }

    public string Ad { get; private set; } = null!;

    /// <summary>Bolgenin coğrafi sinirlarini tanimlayan cokgen (SRID 4326).</summary>
    public Polygon SinirPolygonu { get; private set; } = null!;

    public Guid? SorumluBirimId { get; private set; }

    public Birim? SorumluBirim { get; private set; }

    private Bolge()
    {
    }

    public Bolge(Guid kurumId, string ad, Polygon sinirPolygonu)
    {
        if (kurumId == Guid.Empty)
        {
            throw new ArgumentException("Bolge bir kuruma bagli olmalidir.", nameof(kurumId));
        }

        if (string.IsNullOrWhiteSpace(ad))
        {
            throw new ArgumentException("Bolge adi bos olamaz.", nameof(ad));
        }

        KurumId = kurumId;
        Ad = ad;
        SinirPolygonu = sinirPolygonu ?? throw new ArgumentNullException(nameof(sinirPolygonu));
    }

    /// <summary>
    /// Verilen noktanin bu bolgenin sinirlari icinde olup olmadigini kontrol eder (PostGIS ST_Covers).
    /// Bilerek ST_Contains DEGIL ST_Covers kullanilir: ST_Contains, "geography" kolon tipi icin
    /// PostGIS'te TANIMLI DEGILDIR (sadece "geometry" icin vardir) ve SinirPolygonu geography
    /// olarak saklanir (bkz. BolgeConfiguration). ST_Covers hem geography'de calisir hem de
    /// ST_Contains'in siniri (boundary) kapsam disi birakan durumunu (nokta tam sinirdaysa
    /// "disarida" sayilmasi) daha sezgisel bir sekilde cozer.
    /// </summary>
    public bool NoktayiKapsiyorMu(Point nokta)
    {
        ArgumentNullException.ThrowIfNull(nokta);
        return SinirPolygonu.Covers(nokta);
    }

    public void SorumluBirimAta(Guid birimId)
    {
        if (birimId == Guid.Empty)
        {
            throw new ArgumentException("Gecerli bir birim belirtilmelidir.", nameof(birimId));
        }

        SorumluBirimId = birimId;
    }
}
