using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using SahaGor.Application.Atama;
using SahaGor.Application.Bildirimler;
using SahaGor.Application.Birimler;
using SahaGor.Application.Ekipler;
using SahaGor.Application.Entegrasyonlar;
using SahaGor.Application.Gorevler;
using SahaGor.Application.Kimlik;
using SahaGor.Application.Kurumlar;
using SahaGor.Application.Personeller;
using SahaGor.Infrastructure.Atama;
using SahaGor.Infrastructure.Birimler;
using SahaGor.Infrastructure.Dosyalar;
using SahaGor.Infrastructure.Ekipler;
using SahaGor.Infrastructure.Entegrasyonlar;
using SahaGor.Infrastructure.Gorevler;
using SahaGor.Infrastructure.Kimlik;
using SahaGor.Infrastructure.Kurumlar;
using SahaGor.Infrastructure.Persistence;
using SahaGor.Infrastructure.Personeller;
using SahaGor.Infrastructure.Sms;

namespace SahaGor.Infrastructure;

/// <summary>
/// Infrastructure katmaninin servislerini Api katmanindaki DI konteynerine kaydeden
/// tek giris noktasi. Program.cs sadece "AddInfrastructure" cagirir; boylece Api,
/// Infrastructure'in ic detaylarindan (EF Core, Npgsql vb.) habersiz kalir.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var baglantiDizesi = BaglantiDizesiOlustur(configuration);

        services.AddDbContext<SahaGorDbContext>(options =>
            options.UseNpgsql(baglantiDizesi, npgsqlOptions =>
            {
                // PostGIS geometrilerini (Point/Polygon) NetTopologySuite tipleriyle eslestirir.
                npgsqlOptions.UseNetTopologySuite();

                // Migration geceleri Api projesindeki "Migrations" klasorunde tutulur;
                // Infrastructure katmani migration assembly'sini kendi icinde saklamaz,
                // boylece startup projesi degisse dahi migration konumu acik ve tek yerdedir.
                npgsqlOptions.MigrationsAssembly(typeof(SahaGorDbContext).Assembly.FullName);
            }));

        services.Configure<JwtAyarlari>(configuration.GetSection(JwtAyarlari.BolumAdi));
        services.Configure<DosyaDepolamaAyarlari>(configuration.GetSection(DosyaDepolamaAyarlari.BolumAdi));
        services.Configure<Hat153EntegrasyonAyarlari>(configuration.GetSection(Hat153EntegrasyonAyarlari.BolumAdi));

        services.AddSingleton<ISifreHashleyici, BcryptSifreHashleyici>();
        services.AddSingleton<IJwtTokenUretici, JwtTokenUretici>();
        services.AddSingleton<IFotografDepolamaServisi, YerelDosyaFotografDepolamaServisi>();
        services.AddSingleton<ISmsGonderimServisi, LoglayanSmsGonderimServisi>();
        services.AddScoped<IKimlikDogrulamaServisi, KimlikDogrulamaServisi>();
        services.AddScoped<IGorevTalebiServisi, GorevTalebiServisi>();
        services.AddScoped<IKurumServisi, KurumServisi>();
        services.AddScoped<IBirimServisi, BirimServisi>();
        services.AddScoped<IPersonelServisi, PersonelServisi>();
        services.AddScoped<IEkipServisi, EkipServisi>();
        services.AddScoped<IAtamaMotoru, AtamaMotoru>();
        services.AddScoped<IDisKaynakBasvuruServisi, DisKaynakBasvuruServisi>();

        return services;
    }

    /// <summary>
    /// Baglanti dizesini once appsettings/"ConnectionStrings:Default" uzerinden, o da yoksa
    /// docker-compose ile ayni isimlere sahip ortam degiskenlerinden (POSTGRES_*) guvenli
    /// bir sekilde (NpgsqlConnectionStringBuilder ile, string birlestirme yapmadan) uretir.
    /// </summary>
    private static string BaglantiDizesiOlustur(IConfiguration configuration)
    {
        var yapilandirmadanGelen = configuration.GetConnectionString("Default");
        if (!string.IsNullOrWhiteSpace(yapilandirmadanGelen))
        {
            return yapilandirmadanGelen;
        }

        var sunucu = configuration["POSTGRES_HOST"] ?? "localhost";
        var port = configuration["POSTGRES_PORT"] ?? "5432";
        var veritabani = configuration["POSTGRES_DB"];
        var kullaniciAdi = configuration["POSTGRES_USER"];
        var sifre = configuration["POSTGRES_PASSWORD"];

        if (string.IsNullOrWhiteSpace(veritabani) || string.IsNullOrWhiteSpace(kullaniciAdi) || string.IsNullOrWhiteSpace(sifre))
        {
            throw new InvalidOperationException(
                "Veritabani baglanti bilgileri bulunamadi. 'ConnectionStrings:Default' yapilandirmasini " +
                "veya POSTGRES_DB / POSTGRES_USER / POSTGRES_PASSWORD ortam degiskenlerini tanimlayin.");
        }

        var olusturucu = new NpgsqlConnectionStringBuilder
        {
            Host = sunucu,
            Port = int.Parse(port),
            Database = veritabani,
            Username = kullaniciAdi,
            Password = sifre,
        };

        return olusturucu.ConnectionString;
    }
}
