using Microsoft.EntityFrameworkCore;
using SahaGor.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace SahaGor.IntegrationTests.Gorevler;

/// <summary>
/// SG-420: Birim testlerinde kullanilan EF Core InMemory saglayicisi, PostGIS'in kuresel
/// (spherical) ST_Distance/ST_DWithin hesaplamasini VE geography kolonlarinda gecerli
/// olan/olmayan mekansal fonksiyonlari (orn. ST_Contains) taklit ETMEZ - LINQ ifadesini hic
/// SQL'e cevirmeden kendi bellek ici NetTopologySuite mantigini calistirir. Bu yuzden gercek
/// davranis, gercek bir PostGIS konteyneri (Testcontainers) uzerinde ayrica dogrulanmalidir.
///
/// Bu fixture, testler baslamadan once docker-compose.yml'deki ile AYNI imaji
/// (postgis/postgis:16-3.4-alpine) kullanan gecici bir konteyner ayaga kaldirir, gercek
/// migration'lari uygular ve testler bitince konteyneri tamamen siler. Docker kurulu/erisilir
/// olmayan ortamlarda bu sinifi kullanan testler acikca (skip degil, hata ile) basarisiz olur;
/// CI ortaminda (SG-422) Docker daima mevcut olacaktir.
/// </summary>
public sealed class PostGisTestFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _konteyner = new PostgreSqlBuilder("postgis/postgis:16-3.4-alpine")
        .WithDatabase("sahagor_test")
        .WithUsername("sahagor_test")
        .WithPassword("sadece_test_ortami_sifresi")
        .Build();

    public async Task InitializeAsync()
    {
        await _konteyner.StartAsync();

        await using var dbContext = DbContextOlustur();
        // Gercek migration gecmisi (SG-1'den beri birikmis tum migration'lar dahil) burada
        // gercek bir PostgreSQL/PostGIS'e karsi calistirilir; bu, migration'larin production'da
        // da sorunsuz uygulanacagini dogrulayan ayri bir kabul kriteridir.
        await dbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _konteyner.DisposeAsync();
    }

    /// <summary>Her cagrida, ayni konteynere baglanan YENI bir DbContext ornegi doner.</summary>
    public SahaGorDbContext DbContextOlustur()
    {
        var secenekler = new DbContextOptionsBuilder<SahaGorDbContext>()
            .UseNpgsql(_konteyner.GetConnectionString(), npgsqlOptions =>
            {
                npgsqlOptions.UseNetTopologySuite();
                npgsqlOptions.MigrationsAssembly(typeof(SahaGorDbContext).Assembly.FullName);
            })
            .Options;

        return new SahaGorDbContext(secenekler);
    }
}

/// <summary>
/// Ayni PostGIS konteynerinin bu koleksiyondaki tum test siniflari arasinda PAYLASILMASINI
/// saglar; her test sinifi icin ayri ayri konteyner baslatmak (birkac saniye surdugu icin)
/// gereksiz yere yavaslatir.
/// </summary>
[CollectionDefinition(Adi)]
public sealed class PostGisTestCollection : ICollectionFixture<PostGisTestFixture>
{
    public const string Adi = "PostGIS";
}
