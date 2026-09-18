namespace SahaGor.Infrastructure.Persistence;

/// <summary>
/// EF Core migration seed verilerinde (HasData) kullanilan sabit GUID ve zaman degerleri.
///
/// Onemli: Domain entity kuruculari (orn. "new GorevKategorisi(...)") TemelVarlik uzerinden
/// her cagrildiginda Guid.NewGuid() ile YENI bir Id ve DateTime.UtcNow ile YENI bir zaman
/// damgasi uretir. HasData icinde bu kuruculari dogrudan kullanmak, "dotnet ef migrations add"
/// her calistirildiginda ayni referans veri icin farkli degerler uretilmesine ve gereksiz/
/// yanlis delete+insert migration'lari olusmasina yol acar. Bu yuzden seed veriler, entity
/// kurucusu yerine bu sabit degerlerle ve anonim nesnelerle (bkz. *Configuration siniflari)
/// tanimlanir.
/// </summary>
internal static class BaslangicVerisiSabitleri
{
    /// <summary>Tum seed kayitlari icin ortak, sabit bir olusturma zamani (migration'lar arasi tutarlilik icin).</summary>
    public static readonly DateTime SabitOlusturmaZamaniUtc = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public static class GorevKategorileri
    {
        public static readonly Guid KirikKaldirim = Guid.Parse("00000000-0000-0000-0a01-000000000001");
        public static readonly Guid SokakLambasi = Guid.Parse("00000000-0000-0000-0a01-000000000002");
        public static readonly Guid CopToplama = Guid.Parse("00000000-0000-0000-0a01-000000000003");
        public static readonly Guid BasibosHayvan = Guid.Parse("00000000-0000-0000-0a01-000000000004");
    }

    public static class Organizasyon
    {
        public static readonly Guid OrnekKurum = Guid.Parse("00000000-0000-0000-0a02-000000000001");
        public static readonly Guid SistemYonetimiBirimi = Guid.Parse("00000000-0000-0000-0a02-000000000002");
        public static readonly Guid SistemYoneticisiPersoneli = Guid.Parse("00000000-0000-0000-0a02-000000000003");
    }
}
