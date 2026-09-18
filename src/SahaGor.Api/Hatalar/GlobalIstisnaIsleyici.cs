using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace SahaGor.Api.Hatalar;

/// <summary>
/// Controller'larda ozel olarak yakalanmayan (beklenmeyen) tum istisnalari tek noktadan
/// isler (SG-141). Amac: (1) hatanin tum detaylarini (stack trace dahil) sunucu loguna
/// yazmak, (2) istemciye ASLA stack trace/ic detay sizdirmadan, Turkce ve izlenebilir
/// (izlemeId ile) standart bir ProblemDetails yaniti dondurmek.
/// </summary>
public sealed class GlobalIstisnaIsleyici : IExceptionHandler
{
    private readonly ILogger<GlobalIstisnaIsleyici> _logger;

    public GlobalIstisnaIsleyici(ILogger<GlobalIstisnaIsleyici> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var izlemeId = httpContext.TraceIdentifier;

        _logger.LogError(exception,
            "Islenmeyen bir hata olustu. IzlemeId: {IzlemeId}, Yol: {Yol}, Metot: {Metot}",
            izlemeId, httpContext.Request.Path, httpContext.Request.Method);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        httpContext.Response.ContentType = "application/problem+json";

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Beklenmeyen bir hata olustu",
            Detail = "Isteginiz islenirken beklenmeyen bir sunucu hatasi olustu. Sorun devam ederse " +
                     "asagidaki izleme numarasiyla destek ekibine basvurun.",
            Extensions =
            {
                ["izlemeId"] = izlemeId,
            },
        };

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        // true dondurmek, istisnanin "islendigini" ve pipeline'in burada durdurulmasi
        // gerektigini belirtir; aksi halde ASP.NET Core istisnayi tekrar firlatir.
        return true;
    }
}
