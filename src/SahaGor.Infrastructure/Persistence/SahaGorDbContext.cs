using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using SahaGor.Domain.Ortak;
using SahaGor.Domain.Varliklar;

namespace SahaGor.Infrastructure.Persistence;

/// <summary>
/// SahaGor platformunun EF Core veritabani baglami. Tum entity konfigurasyonlari
/// bu assembly icindeki IEntityTypeConfiguration&lt;T&gt; siniflarindan otomatik yuklenir.
/// </summary>
public class SahaGorDbContext : DbContext
{
    public SahaGorDbContext(DbContextOptions<SahaGorDbContext> options) : base(options)
    {
    }

    public DbSet<Kurum> Kurumlar => Set<Kurum>();

    public DbSet<Birim> Birimler => Set<Birim>();

    public DbSet<Personel> Personeller => Set<Personel>();

    public DbSet<Ekip> Ekipler => Set<Ekip>();

    public DbSet<Bolge> Bolgeler => Set<Bolge>();

    public DbSet<GorevKategorisi> GorevKategorileri => Set<GorevKategorisi>();

    public DbSet<GorevTalebi> GorevTalepleri => Set<GorevTalebi>();

    public DbSet<GorevFotografi> GorevFotograflari => Set<GorevFotografi>();

    public DbSet<GorevDurumGecmisi> GorevDurumGecmisleri => Set<GorevDurumGecmisi>();

    public DbSet<YenilemeJetonu> YenilemeJetonlari => Set<YenilemeJetonu>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Bu assembly icindeki tum IEntityTypeConfiguration<T> siniflarini otomatik uygular;
        // yeni bir entity eklendiginde OnModelCreating'i degistirmeye gerek kalmaz (Open/Closed prensibi).
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SahaGorDbContext).Assembly);

        // Yumusak silinmis (SilindiMi = true) kayitlarin varsayilan sorgulardan
        // otomatik disarida birakilmasi icin tum TemelVarlik turevlerine global filtre uygulanir.
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(TemelVarlik).IsAssignableFrom(entityType.ClrType))
            {
                var yontem = typeof(SahaGorDbContext)
                    .GetMethod(nameof(YumusakSilmeFiltresiOlustur), BindingFlags.NonPublic | BindingFlags.Static)!
                    .MakeGenericMethod(entityType.ClrType);

                var filtre = yontem.Invoke(null, null);
                entityType.SetQueryFilter((LambdaExpression)filtre!);
            }
        }
    }

    private static Expression<Func<TEntity, bool>> YumusakSilmeFiltresiOlustur<TEntity>()
        where TEntity : TemelVarlik
        => entity => !entity.SilindiMi;
}
