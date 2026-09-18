using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SahaGor.Application.Kurumlar;
using SahaGor.Application.Kurumlar.Dtolar;
using SahaGor.Application.Kurumlar.Istisnalar;
using SahaGor.Domain.Enumlar;

namespace SahaGor.Api.Controllers;

/// <summary>Kurum (belediye) yonetimi CRUD uc noktalari (SG-110).</summary>
[ApiController]
[Authorize]
[Route("api/kurumlar")]
public class KurumlarController : ControllerBase
{
    private readonly IKurumServisi _kurumServisi;

    public KurumlarController(IKurumServisi kurumServisi)
    {
        _kurumServisi = kurumServisi;
    }

    [HttpPost]
    [Authorize(Roles = nameof(PersonelRolu.SistemYoneticisi))]
    [ProducesResponseType(typeof(KurumYaniti), StatusCodes.Status201Created)]
    public async Task<ActionResult<KurumYaniti>> Olustur([FromBody] KurumOlusturIstegi istek, CancellationToken iptalToken)
    {
        var yanit = await _kurumServisi.OlusturAsync(istek, iptalToken);
        return CreatedAtAction(nameof(Detay), new { id = yanit.Id }, yanit);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<KurumYaniti>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<KurumYaniti>>> Listele(CancellationToken iptalToken)
    {
        return Ok(await _kurumServisi.ListeleAsync(iptalToken));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(KurumYaniti), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<KurumYaniti>> Detay(Guid id, CancellationToken iptalToken)
    {
        try
        {
            return Ok(await _kurumServisi.DetayGetirAsync(id, iptalToken));
        }
        catch (KurumBulunamadiException hata)
        {
            return Problem(title: "Kurum bulunamadi", detail: hata.Message, statusCode: StatusCodes.Status404NotFound);
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = nameof(PersonelRolu.SistemYoneticisi))]
    [ProducesResponseType(typeof(KurumYaniti), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<KurumYaniti>> Guncelle(Guid id, [FromBody] KurumGuncelleIstegi istek, CancellationToken iptalToken)
    {
        try
        {
            return Ok(await _kurumServisi.GuncelleAsync(id, istek, iptalToken));
        }
        catch (KurumBulunamadiException hata)
        {
            return Problem(title: "Kurum bulunamadi", detail: hata.Message, statusCode: StatusCodes.Status404NotFound);
        }
    }

    [HttpPatch("{id:guid}/durum")]
    [Authorize(Roles = nameof(PersonelRolu.SistemYoneticisi))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DurumGuncelle(Guid id, [FromBody] DurumGuncelleIstegi istek, CancellationToken iptalToken)
    {
        try
        {
            await _kurumServisi.DurumGuncelleAsync(id, istek.AktifMi, iptalToken);
            return NoContent();
        }
        catch (KurumBulunamadiException hata)
        {
            return Problem(title: "Kurum bulunamadi", detail: hata.Message, statusCode: StatusCodes.Status404NotFound);
        }
    }
}

/// <summary>PATCH .../durum istegi govdesi; Kurum/Birim/Personel/Ekip controller'larinca ortak kullanilir.</summary>
public sealed record DurumGuncelleIstegi(bool AktifMi);
