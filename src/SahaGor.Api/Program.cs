using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SahaGor.Api;
using SahaGor.Api.ArkaPlanIsleri;
using SahaGor.Api.Hatalar;
using SahaGor.Api.Hublar;
using SahaGor.Api.Saglik;
using SahaGor.Application.Bildirimler;
using SahaGor.Infrastructure;
using SahaGor.Infrastructure.Entegrasyonlar;
using SahaGor.Infrastructure.Gorevler;
using SahaGor.Infrastructure.Kimlik;
using SahaGor.Infrastructure.Saglik;
using Serilog;

// Serilog'un kendisi baslatilamadan once (orn. yapilandirma hatasi) olusan hatalari da
// yakalayabilmek icin, tam yapilandirma yuklenene kadar gecici bir "bootstrap" logger kullanilir.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("SahaGor Api baslatiliyor...");

    var builder = WebApplication.CreateBuilder(args);

    // appsettings.json/"Serilog" bolumunden okunan asil yapilandirma; ortam bazli log
    // seviyeleri (Development/Production) kod degistirmeden appsettings ile ayarlanabilir.
    builder.Host.UseSerilog((context, services, loggerConfiguration) => loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    // Add services to the container.

    builder.Services.AddControllers()
        .AddJsonOptions(jsonOptions =>
        {
            // Enum degerleri (Durum, Oncelik, Kaynak vb.) JSON'da "1" gibi sayilar yerine
            // "Atandi" gibi okunabilir metin olarak dondurulur; mobil/web istemcilerin
            // sayisal degerleri ezbere bilmesine gerek kalmaz.
            jsonOptions.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(swaggerOptions =>
    {
        // Swagger UI uzerinden "Authorize" ile JWT girilip korumali uc noktalarin
        // test edilebilmesi icin Bearer sema tanimi eklenir.
        swaggerOptions.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Ornek: erisim tokenini 'Bearer {token}' seklinde girmeden, sadece token degerini yazmaniz yeterlidir.",
        });

        swaggerOptions.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
                Array.Empty<string>()
            },
        });
    });

    // Veritabani (PostgreSQL/PostGIS) baglantisi ve EF Core kaydi Infrastructure katmaninda
    // tek noktadan yonetilir; Api katmani baglanti dizesinin nasil olustugunu bilmez.
    builder.Services.AddInfrastructure(builder.Configuration);

    // JWT dogrulama parametreleri, ayni JwtAyarlari yapilandirmasindan (appsettings/ortam degiskeni)
    // okunur; token uretimi (Infrastructure) ile token dogrulamasi (Api) ayni degerleri kullanir.
    var jwtAyarlari = builder.Configuration.GetSection(JwtAyarlari.BolumAdi).Get<JwtAyarlari>()
        ?? throw new InvalidOperationException("'Jwt' yapilandirma bolumu bulunamadi.");
    jwtAyarlari.Dogrula();

    // 153 hatti webhook'unun HMAC imza dogrulamasinda kullandigi paylasilan gizli anahtar da,
    // uygulama gecerli bir anahtar olmadan (orn. production'da unutulmus bir ayar yuzunden)
    // ayakta kalmasin diye baslangicta (fail-fast) dogrulanir.
    var hat153Ayarlari = builder.Configuration.GetSection(Hat153EntegrasyonAyarlari.BolumAdi).Get<Hat153EntegrasyonAyarlari>()
        ?? throw new InvalidOperationException($"'{Hat153EntegrasyonAyarlari.BolumAdi}' yapilandirma bolumu bulunamadi.");
    hat153Ayarlari.Dogrula();

    // SLA alarm tarama araliginin/esiginin gecerli olmasi da baslangicta dogrulanir; aksi halde
    // (orn. yanlislikla 0 saniyelik bir tarama araligi girilirse) sistem sessizce yanlis davranir.
    var slaAlarmAyarlari = builder.Configuration.GetSection(SlaAlarmAyarlari.BolumAdi).Get<SlaAlarmAyarlari>()
        ?? throw new InvalidOperationException($"'{SlaAlarmAyarlari.BolumAdi}' yapilandirma bolumu bulunamadi.");
    slaAlarmAyarlari.Dogrula();

    // Web admin paneli (Next.js) ile bu API farkli origin'lerde (farkli port dahi olsa)
    // calisir; CORS yapilandirilmadan tarayicidan yapilan hicbir kimlik dogrulamali istek
    // basariya ulasamaz (SG-421'de gercek bir tarayiciyla E2E test yazilirken bulunan hata).
    const string CorsPolitikaAdi = "SahaGorWebPolitikasi";
    var corsAyarlari = builder.Configuration.GetSection(CorsAyarlari.BolumAdi).Get<CorsAyarlari>()
        ?? throw new InvalidOperationException($"'{CorsAyarlari.BolumAdi}' yapilandirma bolumu bulunamadi.");
    corsAyarlari.Dogrula();

    builder.Services.AddCors(corsOptions =>
    {
        corsOptions.AddPolicy(CorsPolitikaAdi, policy =>
        {
            policy.WithOrigins(corsAyarlari.IzinVerilenKaynaklar)
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
    });

    builder.Services
        .AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtAyarlari.Yayinlayan,
                ValidateAudience = true,
                ValidAudience = jwtAyarlari.Kitle,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtAyarlari.Anahtar)),
                ValidateLifetime = true,
                // Sunucular arasi saat farkini tolere etmek icin kucuk bir pay birakilir;
                // sifir olsaydi milisaniyelik saat kaymalarinda dahi gecerli tokenlar reddedilebilirdi.
                ClockSkew = TimeSpan.FromSeconds(30),
            };

            // SignalR, tarayici WebSocket API'si geregi Authorization header'i degil,
            // baglanti URL'sine "access_token" query string parametresi ekleyerek JWT gonderir.
            // Bu olmadan hub baglantilari her zaman 401 ile reddedilir.
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var erisimTokeni = context.Request.Query["access_token"];
                    var yol = context.HttpContext.Request.Path;

                    if (!string.IsNullOrEmpty(erisimTokeni) && yol.StartsWithSegments("/hub"))
                    {
                        context.Token = erisimTokeni;
                    }

                    return Task.CompletedTask;
                },
            };
        });

    builder.Services.AddAuthorization();

    // Beklenmeyen istisnalarin merkezi olarak yakalanip Turkce+ProblemDetails formatinda
    // donulmesi icin (SG-141). "AddProblemDetails" olmadan handler'in urettigi govde,
    // ASP.NET Core'un varsayilan ProblemDetails serilestirme kurallarini kullanmaz.
    builder.Services.AddExceptionHandler<GlobalIstisnaIsleyici>();
    builder.Services.AddProblemDetails();

    // Canli harita/komuta paneli icin SignalR alt yapisi (SG-140). Somut yayin mekanizmasi
    // (SignalRBildirimYayinlayici), Application katmaninin tanimladigi soyutlamalari karsilar.
    builder.Services.AddSignalR();
    builder.Services.AddScoped<IGorevBildirimYayinlayici, SignalRBildirimYayinlayici>();
    builder.Services.AddScoped<IEkipBildirimYayinlayici, SignalRBildirimYayinlayici>();
    builder.Services.AddScoped<ISlaAlarmYayinlayici, SignalRBildirimYayinlayici>();

    // SG-410: acik gorevleri periyodik olarak SLA esiklerine gore tarayip alarm yayinlayan
    // arka plan servisi. Uygulama ile birlikte baslar ve host kapatilana kadar calisir.
    builder.Services.AddHostedService<SlaAlarmArkaPlanServisi>();

    // Veritabani/PostGIS baglanti durumunun dogrulanabilmesi icin (SG-142).
    builder.Services.AddHealthChecks()
        .AddCheck<PostGisSaglikKontrolu>("postgresql-postgis");

    var app = builder.Build();

    // Beklenmeyen hatalari yakalayan middleware, pipeline'in EN BASINDA olmalidir;
    // aksi halde ondan once calisan bir middleware'deki hata yakalanamaz.
    app.UseExceptionHandler();

    // Her HTTP istegini (yol, metot, durum kodu, sure) yapilandirilmis (structured) log
    // olarak Serilog'a yazar; hata ayiklama ve performans izleme icin temel veri kaynagidir.
    app.UseSerilogRequestLogging();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }
    else
    {
        // SG-423 (OWASP A05 - Security Misconfiguration): tarayicilara, HTTPS baglantilarinin
        // gelecekteki isteklerde de zorunlu tutulmasi gerektigini bildirir. Gelistirme
        // ortaminda (genelde kendinden imzali/HTTPS'siz) sorun cikarmamasi icin sadece
        // production'da (Development disinda) etkinlestirilir.
        app.UseHsts();
    }

    // SG-423: API varsayilan olarak hicbir guvenlik header'i eklemez. Bu asgari, dusuk
    // riskli fakat standart set (OWASP Secure Headers Project) tarayici tabanli bazi
    // saldiri siniflarina karsi ek bir savunma katmani saglar. Content-Security-Policy
    // bilerek eklenmedi: bu bir JSON API'dir (HTML render etmez), CSP esas olarak
    // Swagger UI (sadece Development'ta acik) icin anlamli olur ve yanlis yapilandirilirsa
    // Swagger'i bozabilir.
    app.Use(async (context, next) =>
    {
        context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
        context.Response.Headers.Append("X-Frame-Options", "DENY");
        context.Response.Headers.Append("Referrer-Policy", "no-referrer");
        await next();
    });

    app.UseHttpsRedirection();

    // CORS, kimlik dogrulama/yetkilendirmeden ONCE calismalidir; aksi halde on-kontrol
    // (preflight OPTIONS) istekleri [Authorize] tarafindan reddedilir.
    app.UseCors(CorsPolitikaAdi);

    // Kimlik dogrulama, yetkilendirmeden once calismalidir; aksi halde User.Identity
    // her zaman dogrulanmamis (anonim) kalir ve [Authorize] her istegi reddeder.
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();
    app.MapHub<GorevTalebiHub>("/hub/gorevler");
    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        ResponseWriter = SaglikKontroluYaniti.YazAsync,
    });

    app.Run();
}
catch (Exception hata)
{
    Log.Fatal(hata, "SahaGor Api baslatilirken kurtarilamayan bir hata olustu.");
}
finally
{
    Log.CloseAndFlush();
}
