using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using SahaGor.Api.Hatalar;

namespace SahaGor.IntegrationTests.Hatalar;

/// <summary>
/// SG-141 kabul kriterini dogrular: islenmeyen bir istisna, istemciye ASLA stack trace
/// sizdirmadan, standart Turkce bir ProblemDetails yaniti (500) olarak donmelidir.
/// </summary>
public class GlobalIstisnaIsleyiciTests
{
    [Fact]
    public async Task TryHandleAsync_StackTrace_Icermeyen_Standart_ProblemDetails_Doner()
    {
        var isleyici = new GlobalIstisnaIsleyici(NullLogger<GlobalIstisnaIsleyici>.Instance);

        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();

        var gizliBilgiIcerenHata = new InvalidOperationException("Baglanti dizesi: Host=gizli-sunucu;Password=cok-gizli-sifre");

        var islendiMi = await isleyici.TryHandleAsync(httpContext, gizliBilgiIcerenHata, CancellationToken.None);

        Assert.True(islendiMi);
        Assert.Equal(StatusCodes.Status500InternalServerError, httpContext.Response.StatusCode);

        httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        var govde = await new StreamReader(httpContext.Response.Body).ReadToEndAsync();

        var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(govde,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.NotNull(problemDetails);
        Assert.Equal(500, problemDetails!.Status);

        // En kritik dogrulama: yanit govdesinde ne istisnanin mesaji, ne stack trace,
        // ne de baglanti dizesi gibi hassas bilgiler yer almamali.
        Assert.DoesNotContain("cok-gizli-sifre", govde);
        Assert.DoesNotContain("gizli-sunucu", govde);
        Assert.DoesNotContain("InvalidOperationException", govde);
        Assert.DoesNotContain("at SahaGor", govde);
    }

    [Fact]
    public async Task TryHandleAsync_Yanitta_Izlenebilir_Bir_IzlemeId_Bulunur()
    {
        var isleyici = new GlobalIstisnaIsleyici(NullLogger<GlobalIstisnaIsleyici>.Instance);

        var httpContext = new DefaultHttpContext { TraceIdentifier = "test-izleme-id-12345" };
        httpContext.Response.Body = new MemoryStream();

        await isleyici.TryHandleAsync(httpContext, new Exception("herhangi bir hata"), CancellationToken.None);

        httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        var govde = await new StreamReader(httpContext.Response.Body).ReadToEndAsync();

        Assert.Contains("test-izleme-id-12345", govde);
    }
}
