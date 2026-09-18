using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SahaGor.Api.Controllers;
using SahaGor.Application.Entegrasyonlar;
using SahaGor.Application.Entegrasyonlar.Dtolar;
using SahaGor.Application.Entegrasyonlar.Istisnalar;
using SahaGor.Application.Gorevler.Dtolar;
using SahaGor.Domain.Enumlar;
using SahaGor.Infrastructure.Entegrasyonlar;

namespace SahaGor.IntegrationTests.Entegrasyonlar;

/// <summary>
/// SG-401 kabul kriteri: 153 webhook'u, sadece paylasilan gizli anahtarla dogru sekilde
/// HMAC-SHA256 imzalanmis istekleri kabul eder; imzasiz veya yanlis imzali istekler 401 alir.
/// </summary>
public class Hat153EntegrasyonControllerTests
{
    private const string GizliAnahtar = "test-ortami-icin-16-karakterden-uzun-gizli-anahtar";

    /// <summary>Testlerde gercek veritabanina/GorevTalebiServisi'ne ihtiyac olmadan davranisi kontrol eden sahte servis.</summary>
    private sealed class SahteDisKaynakBasvuruServisi : IDisKaynakBasvuruServisi
    {
        public Hat153BasvuruIstegi? AlinanIstek { get; private set; }
        public Exception? FirlatilacakHata { get; set; }

        public Task<GorevTalebiDetayYaniti> Hat153BasvurusuIsleAsync(Hat153BasvuruIstegi istek,
            CancellationToken iptalToken = default)
        {
            AlinanIstek = istek;
            if (FirlatilacakHata is not null)
            {
                throw FirlatilacakHata;
            }

            var yaniti = new GorevTalebiDetayYaniti(Guid.NewGuid(), istek.Baslik, istek.Aciklama, istek.KategoriAdi,
                nameof(GorevDurumu.Atanamadi), nameof(GorevOnceligi.Normal), nameof(GorevKaynagi.Hat153),
                istek.Enlem, istek.Boylam, null, null, null, istek.BildirenTelefonu, DateTime.UtcNow,
                DateTime.UtcNow.AddHours(8), false, Array.Empty<GorevDurumGecmisiYaniti>(),
                Array.Empty<GorevFotografiYaniti>());

            return Task.FromResult(yaniti);
        }
    }

    private static string GecerliImzaHesapla(string govde)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(GizliAnahtar));
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(govde)));
    }

    /// <summary>
    /// Controller'in "Problem()" yardimcisi, HttpContext.RequestServices uzerinden
    /// ProblemDetailsFactory arar; bu yuzden minimal bir MVC servis saglayicisi kurulur.
    /// </summary>
    private static IServiceProvider ProblemDetailsServisSaglayicisiOlustur()
    {
        var servisler = new ServiceCollection();
        servisler.AddLogging();
        servisler.AddControllers();
        return servisler.BuildServiceProvider();
    }

    private static (Hat153EntegrasyonController Controller, SahteDisKaynakBasvuruServisi Servis) ControllerOlustur(
        string govde, string? imzaHeaderDegeri)
    {
        var servis = new SahteDisKaynakBasvuruServisi();
        var ayarlar = Options.Create(new Hat153EntegrasyonAyarlari { PaylasilanGizliAnahtar = GizliAnahtar });
        var controller = new Hat153EntegrasyonController(servis, ayarlar,
            NullLogger<Hat153EntegrasyonController>.Instance);

        var httpContext = new DefaultHttpContext
        {
            RequestServices = ProblemDetailsServisSaglayicisiOlustur(),
        };
        httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(govde));
        httpContext.Request.ContentType = "application/json";
        if (imzaHeaderDegeri is not null)
        {
            httpContext.Request.Headers["X-Signature"] = imzaHeaderDegeri;
        }

        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        return (controller, servis);
    }

    private const string OrnekGovde =
        """{"referansNo":"153-2026-000123","baslik":"Kaldirim cokmus","aciklama":"Yaya gecidinde tehlike","enlem":41.0,"boylam":29.0,"bildirenTelefonu":"5551234567","kategoriAdi":"Kırık Kaldırım"}""";

    [Fact]
    public async Task Basvuru_Gecerli_Imzayla_Servisi_Cagirip_201_Doner()
    {
        var (controller, servis) = ControllerOlustur(OrnekGovde, GecerliImzaHesapla(OrnekGovde));

        var sonuc = await controller.Basvuru(CancellationToken.None);

        var olusturuldu = Assert.IsType<CreatedAtActionResult>(sonuc);
        Assert.Equal(StatusCodes.Status201Created, olusturuldu.StatusCode);
        Assert.NotNull(servis.AlinanIstek);
        Assert.Equal("153-2026-000123", servis.AlinanIstek!.ReferansNo);
    }

    [Fact]
    public async Task Basvuru_Imza_Header_Yoksa_401_Doner_Ve_Servis_Cagrilmaz()
    {
        var (controller, servis) = ControllerOlustur(OrnekGovde, imzaHeaderDegeri: null);

        var sonuc = await controller.Basvuru(CancellationToken.None);

        var problemSonucu = Assert.IsType<ObjectResult>(sonuc);
        Assert.Equal(StatusCodes.Status401Unauthorized, problemSonucu.StatusCode);
        Assert.Null(servis.AlinanIstek);
    }

    [Fact]
    public async Task Basvuru_Yanlis_Imzayla_401_Doner_Ve_Servis_Cagrilmaz()
    {
        var (controller, servis) = ControllerOlustur(OrnekGovde,
            imzaHeaderDegeri: "00112233445566778899aabbccddeeff00112233445566778899aabbccddee");

        var sonuc = await controller.Basvuru(CancellationToken.None);

        var problemSonucu = Assert.IsType<ObjectResult>(sonuc);
        Assert.Equal(StatusCodes.Status401Unauthorized, problemSonucu.StatusCode);
        Assert.Null(servis.AlinanIstek);
    }

    [Fact]
    public async Task Basvuru_Hex_Olmayan_Imzayla_401_Doner()
    {
        var (controller, servis) = ControllerOlustur(OrnekGovde, imzaHeaderDegeri: "bu-hex-degil-gecersiz-imza");

        var sonuc = await controller.Basvuru(CancellationToken.None);

        var problemSonucu = Assert.IsType<ObjectResult>(sonuc);
        Assert.Equal(StatusCodes.Status401Unauthorized, problemSonucu.StatusCode);
        Assert.Null(servis.AlinanIstek);
    }

    [Fact]
    public async Task Basvuru_Gecersiz_Json_Ile_400_Doner()
    {
        const string gecersizGovde = "{bu gecerli bir json degil";
        var (controller, servis) = ControllerOlustur(gecersizGovde, GecerliImzaHesapla(gecersizGovde));

        var sonuc = await controller.Basvuru(CancellationToken.None);

        var problemSonucu = Assert.IsType<ObjectResult>(sonuc);
        Assert.Equal(StatusCodes.Status400BadRequest, problemSonucu.StatusCode);
        Assert.Null(servis.AlinanIstek);
    }

    [Fact]
    public async Task Basvuru_Bilinmeyen_Kategori_Icin_404_Doner()
    {
        var (controller, servis) = ControllerOlustur(OrnekGovde, GecerliImzaHesapla(OrnekGovde));
        servis.FirlatilacakHata = new GorevKategorisiAdiylaBulunamadiException("Kırık Kaldırım");

        var sonuc = await controller.Basvuru(CancellationToken.None);

        var problemSonucu = Assert.IsType<ObjectResult>(sonuc);
        Assert.Equal(StatusCodes.Status404NotFound, problemSonucu.StatusCode);
    }
}
