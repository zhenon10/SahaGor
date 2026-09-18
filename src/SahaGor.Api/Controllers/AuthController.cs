using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SahaGor.Application.Kimlik;
using SahaGor.Application.Kimlik.Dtolar;
using SahaGor.Application.Kimlik.Istisnalar;

namespace SahaGor.Api.Controllers;

/// <summary>Kimlik dogrulama uc noktalari: giris, token yenileme ve mevcut kullanici bilgisi (SG-120, SG-122).</summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IKimlikDogrulamaServisi _kimlikDogrulamaServisi;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IKimlikDogrulamaServisi kimlikDogrulamaServisi, ILogger<AuthController> logger)
    {
        _kimlikDogrulamaServisi = kimlikDogrulamaServisi;
        _logger = logger;
    }

    /// <summary>Kullanici adi/sifre ile giris yapar; basarili olursa erisim ve yenileme jetonu dondurur.</summary>
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(GirisYaniti), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status423Locked)]
    public async Task<ActionResult<GirisYaniti>> GirisYap([FromBody] GirisIstegi istek, CancellationToken iptalToken)
    {
        try
        {
            var yanit = await _kimlikDogrulamaServisi.GirisYapAsync(istek, iptalToken);
            return Ok(yanit);
        }
        catch (HesapKilitliException hata)
        {
            return Problem(
                title: "Hesap kilitli",
                detail: hata.Message,
                statusCode: StatusCodes.Status423Locked);
        }
        catch (HesapPasifException hata)
        {
            return Problem(
                title: "Hesap pasif",
                detail: hata.Message,
                statusCode: StatusCodes.Status401Unauthorized);
        }
        catch (GecersizGirisException hata)
        {
            return Problem(
                title: "Giris basarisiz",
                detail: hata.Message,
                statusCode: StatusCodes.Status401Unauthorized);
        }
    }

    /// <summary>Suresi dolmak uzere olan erisim tokenini, gecerli bir yenileme tokeni ile yeniler.</summary>
    [AllowAnonymous]
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(GirisYaniti), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<GirisYaniti>> TokenYenile([FromBody] YenilemeIstegi istek, CancellationToken iptalToken)
    {
        try
        {
            var yanit = await _kimlikDogrulamaServisi.TokenYenileAsync(istek, iptalToken);
            return Ok(yanit);
        }
        catch (GecersizYenilemeJetonuException hata)
        {
            return Problem(
                title: "Yenileme basarisiz",
                detail: hata.Message,
                statusCode: StatusCodes.Status401Unauthorized);
        }
    }

    /// <summary>JWT dogrulama hattinin uctan uca calistigini gormek icin: giris yapmis kullanicinin kimlik bilgileri.</summary>
    [Authorize]
    [HttpGet("ben")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Ben()
    {
        return Ok(new
        {
            personelId = User.FindFirstValue(ClaimTypes.NameIdentifier),
            kullaniciAdi = User.FindFirstValue(ClaimTypes.Name),
            rol = User.FindFirstValue(ClaimTypes.Role),
        });
    }
}
