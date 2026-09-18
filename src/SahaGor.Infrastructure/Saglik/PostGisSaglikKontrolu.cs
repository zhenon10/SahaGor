using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SahaGor.Infrastructure.Persistence;

namespace SahaGor.Infrastructure.Saglik;

/// <summary>
/// Sadece Postgres baglantisini degil, PostGIS uzantisinin da yuklu ve calisir durumda
/// oldugunu dogrulayan saglik kontrolu (SG-142). Basit bir "SELECT 1" yeterli olmazdi,
/// cunku uzanti eksik olsa dahi baglanti kurulabilir ama coğrafi sorgular patlar.
/// </summary>
public sealed class PostGisSaglikKontrolu : IHealthCheck
{
    private readonly SahaGorDbContext _dbContext;

    public PostGisSaglikKontrolu(SahaGorDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // EF Core'un skaler SqlQuery<T> ozelligi, sonuc kolonunun "Value" olarak
            // adlandirilmis olmasini BEKLER (kendi ic sorgusunu "SELECT t."Value" FROM (...) AS t"
            // seklinde sarar); PostGIS_version() fonksiyonunun ham sutun adi bu degildir, bu
            // yuzden acikca "Value" olarak takma ad verilmelidir - aksi halde gercek Postgres'e
            // karsi "column t.Value does not exist" hatasiyla patlar (InMemory saglayicisinda
            // SqlQuery zaten desteklenmedigi/calisitirilmadigi icin bu daha once fark edilmemisti).
            var postgisSurumu = await _dbContext.Database
                .SqlQuery<string>($"SELECT PostGIS_version() AS \"Value\"")
                .FirstOrDefaultAsync(cancellationToken);

            var veriler = new Dictionary<string, object>
            {
                ["postgisSurumu"] = postgisSurumu ?? "bilinmiyor",
            };

            return HealthCheckResult.Healthy("PostgreSQL/PostGIS baglantisi saglikli.", veriler);
        }
        catch (Exception hata)
        {
            return HealthCheckResult.Unhealthy("PostgreSQL/PostGIS baglantisi kurulamadi veya PostGIS uzantisi yuklu degil.", hata);
        }
    }
}
