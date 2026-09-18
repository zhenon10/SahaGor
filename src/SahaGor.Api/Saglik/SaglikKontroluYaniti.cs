using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SahaGor.Api.Saglik;

/// <summary>
/// /health uc noktasinin varsayilan (duz metin "Healthy/Unhealthy") yaniti yerine,
/// her bir kontrolun durumunu ve detayini iceren okunabilir bir JSON dondurur (SG-142).
/// </summary>
public static class SaglikKontroluYaniti
{
    private static readonly JsonSerializerOptions JsonAyarlari = new() { WriteIndented = true };

    public static Task YazAsync(HttpContext httpContext, HealthReport rapor)
    {
        httpContext.Response.ContentType = "application/json; charset=utf-8";

        var yanit = new
        {
            genelDurum = rapor.Status.ToString(),
            toplamSureMs = rapor.TotalDuration.TotalMilliseconds,
            kontroller = rapor.Entries.Select(kayit => new
            {
                ad = kayit.Key,
                durum = kayit.Value.Status.ToString(),
                aciklama = kayit.Value.Description,
                sureMs = kayit.Value.Duration.TotalMilliseconds,
                veriler = kayit.Value.Data,
                hata = kayit.Value.Exception?.Message,
            }),
        };

        return httpContext.Response.WriteAsync(JsonSerializer.Serialize(yanit, JsonAyarlari));
    }
}
