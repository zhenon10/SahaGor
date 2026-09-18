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
            var postgisSurumu = await _dbContext.Database
                .SqlQuery<string>($"SELECT PostGIS_version()")
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
