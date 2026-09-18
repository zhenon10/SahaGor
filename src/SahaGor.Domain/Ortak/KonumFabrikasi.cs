using NetTopologySuite;
using NetTopologySuite.Geometries;

namespace SahaGor.Domain.Ortak;

/// <summary>
/// PostGIS ile uyumlu (SRID 4326 / WGS84) coğrafi nokta nesnelerini tutarli bir sekilde
/// uretmek icin kullanilan fabrika. Enlem/boylam dogrulamasi tek noktadan yapilir (DRY).
/// </summary>
public static class KonumFabrikasi
{
    /// <summary>WGS84 - Dunya genelinde GPS koordinatlarinin standart referans sistemi.</summary>
    public const int WGS84SridNumarasi = 4326;

    private static readonly GeometryFactory GeometriFabrikasi =
        NtsGeometryServices.Instance.CreateGeometryFactory(WGS84SridNumarasi);

    /// <summary>
    /// Verilen enlem/boylam degerlerinden PostGIS'e yazilabilecek bir Point nesnesi olusturur.
    /// </summary>
    /// <param name="enlem">Latitude, -90 ile 90 arasinda olmalidir.</param>
    /// <param name="boylam">Longitude, -180 ile 180 arasinda olmalidir.</param>
    public static Point NoktaOlustur(double enlem, double boylam)
    {
        KoordinatlariDogrula(enlem, boylam);

        // NetTopologySuite/PostGIS standardinda Coordinate(X, Y) sirasi kullanilir;
        // X = boylam (longitude), Y = enlem (latitude) anlamina gelir. Bu sira ters
        // yazilirsa konumlar haritada okyanus ortasinda gibi yanlis yerlerde gorunur.
        return GeometriFabrikasi.CreatePoint(new Coordinate(boylam, enlem));
    }

    public static void KoordinatlariDogrula(double enlem, double boylam)
    {
        if (enlem is < -90 or > 90)
        {
            throw new ArgumentOutOfRangeException(nameof(enlem), enlem, "Enlem (latitude) -90 ile 90 arasinda olmalidir.");
        }

        if (boylam is < -180 or > 180)
        {
            throw new ArgumentOutOfRangeException(nameof(boylam), boylam, "Boylam (longitude) -180 ile 180 arasinda olmalidir.");
        }
    }
}
