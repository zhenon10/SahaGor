using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SahaGor.Application.Entegrasyonlar;
using SahaGor.Application.Entegrasyonlar.Dtolar;
using SahaGor.Application.Entegrasyonlar.Istisnalar;
using SahaGor.Application.Gorevler.Dtolar;
using SahaGor.Infrastructure.Entegrasyonlar;

namespace SahaGor.Api.Controllers;

/// <summary>
/// 153/CIMER hattinin sunucudan sunucuya cagirdigi, JWT TASIMAYAN webhook uc noktasi (SG-401).
/// Kimlik dogrulamasi klasik Authorization header'i yerine, istek govdesinin paylasilan bir
/// gizli anahtarla HMAC-SHA256 imzalanmasiyla yapilir; imza "X-Signature" header'inda
/// hex-encoded olarak beklenir. Bu yuzden bu controller kasitli olarak [Authorize] ZINCIRINE
/// DAHIL DEGILDIR; guvenligi tamamen imza dogrulamasindan gelir.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/entegrasyonlar/hat153")]
public class Hat153EntegrasyonController : ControllerBase
{
    private const string ImzaHeaderAdi = "X-Signature";

    private static readonly JsonSerializerOptions SerilestirmeAyarlari = new(JsonSerializerDefaults.Web);

    private readonly IDisKaynakBasvuruServisi _disKaynakBasvuruServisi;
    private readonly Hat153EntegrasyonAyarlari _ayarlar;
    private readonly ILogger<Hat153EntegrasyonController> _logger;

    public Hat153EntegrasyonController(IDisKaynakBasvuruServisi disKaynakBasvuruServisi,
        IOptions<Hat153EntegrasyonAyarlari> ayarlar, ILogger<Hat153EntegrasyonController> logger)
    {
        _disKaynakBasvuruServisi = disKaynakBasvuruServisi;
        _ayarlar = ayarlar.Value;
        _logger = logger;
    }

    /// <summary>153 sisteminden gelen yeni bir vatandas basvurusunu alir ve bir GorevTalebi'ne cevirir.</summary>
    [HttpPost("basvurular")]
    [ProducesResponseType(typeof(GorevTalebiDetayYaniti), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Basvuru(CancellationToken iptalToken)
    {
        // Imza, govde henuz JSON'a cozumlenmeden, HAM baytlar uzerinden hesaplanmalidir;
        // aksi halde alan sirasi/bosluk gibi kucuk farklar imzayi gecersiz kilar. Bu yuzden
        // model binding ([FromBody]) yerine govde elle okunur.
        Request.EnableBuffering();

        string hamGovde;
        using (var okuyucu = new StreamReader(Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false,
            bufferSize: 4096, leaveOpen: true))
        {
            hamGovde = await okuyucu.ReadToEndAsync(iptalToken);
        }

        Request.Body.Position = 0;

        if (!Request.Headers.TryGetValue(ImzaHeaderAdi, out var gelenImzaDegerleri) ||
            string.IsNullOrWhiteSpace(gelenImzaDegerleri.ToString()))
        {
            _logger.LogWarning("153 webhook istegi '{HeaderAdi}' imza header'i olmadan reddedildi.", ImzaHeaderAdi);
            return Problem(title: "Imza eksik", detail: $"'{ImzaHeaderAdi}' header'i zorunludur.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        if (!ImzaGecerliMi(hamGovde, gelenImzaDegerleri.ToString()))
        {
            _logger.LogWarning("153 webhook istegi gecersiz HMAC-SHA256 imzasiyla reddedildi.");
            return Problem(title: "Imza gecersiz", detail: "HMAC-SHA256 imza dogrulamasi basarisiz oldu.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        Hat153BasvuruIstegi? istek;
        try
        {
            istek = JsonSerializer.Deserialize<Hat153BasvuruIstegi>(hamGovde, SerilestirmeAyarlari);
        }
        catch (JsonException hata)
        {
            _logger.LogWarning(hata, "153 webhook istek govdesi gecerli bir JSON olarak cozumlenemedi.");
            return Problem(title: "Govde cozumlenemedi", detail: "Istek govdesi gecerli bir JSON degil.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (istek is null)
        {
            return Problem(title: "Govde bos", detail: "Istek govdesi bos olamaz.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        try
        {
            var yanit = await _disKaynakBasvuruServisi.Hat153BasvurusuIsleAsync(istek, iptalToken);
            return CreatedAtAction(nameof(GorevlerController.Detay), "Gorevler", new { id = yanit.Id }, yanit);
        }
        catch (GorevKategorisiAdiylaBulunamadiException hata)
        {
            _logger.LogWarning(hata, "153 basvurusu ({ReferansNo}), taninmayan kategori adi '{KategoriAdi}' nedeniyle reddedildi.",
                istek.ReferansNo, istek.KategoriAdi);
            return Problem(title: "Kategori bulunamadi", detail: hata.Message, statusCode: StatusCodes.Status404NotFound);
        }
    }

    /// <summary>
    /// Ham istek govdesini, paylasilan gizli anahtarla HMAC-SHA256 ile yeniden hesaplayip
    /// gelen imzayla sabit-zamanli (timing-attack guvenli) karsilastirir.
    /// </summary>
    private bool ImzaGecerliMi(string hamGovde, string gelenImzaHex)
    {
        byte[] gelenImzaBaytlari;
        try
        {
            gelenImzaBaytlari = Convert.FromHexString(gelenImzaHex.Trim());
        }
        catch (FormatException)
        {
            return false;
        }

        var anahtarBaytlari = Encoding.UTF8.GetBytes(_ayarlar.PaylasilanGizliAnahtar);
        var govdeBaytlari = Encoding.UTF8.GetBytes(hamGovde);

        using var hmac = new HMACSHA256(anahtarBaytlari);
        var hesaplananImzaBaytlari = hmac.ComputeHash(govdeBaytlari);

        return CryptographicOperations.FixedTimeEquals(hesaplananImzaBaytlari, gelenImzaBaytlari);
    }
}
