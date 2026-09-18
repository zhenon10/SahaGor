using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SahaGor.Application.Birimler;
using SahaGor.Application.Birimler.Dtolar;
using SahaGor.Application.Birimler.Istisnalar;
using SahaGor.Application.Kurumlar.Istisnalar;
using SahaGor.Domain.Enumlar;

namespace SahaGor.Api.Controllers;

/// <summary>Birim/mudurluk yonetimi CRUD uc noktalari (SG-110).</summary>
[ApiController]
[Authorize]
[Route("api/birimler")]
public class BirimlerController : ControllerBase
{
    private readonly IBirimServisi _birimServisi;

    public BirimlerController(IBirimServisi birimServisi)
    {
        _birimServisi = birimServisi;
    }

    [HttpPost]
    [Authorize(Roles = nameof(PersonelRolu.SistemYoneticisi))]
    [ProducesResponseType(typeof(BirimYaniti), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BirimYaniti>> Olustur([FromBody] BirimOlusturIstegi istek, CancellationToken iptalToken)
    {
        try
        {
            var yanit = await _birimServisi.OlusturAsync(istek, iptalToken);
            return CreatedAtAction(nameof(Detay), new { id = yanit.Id }, yanit);
        }
        catch (KurumBulunamadiException hata)
        {
            return Problem(title: "Kurum bulunamadi", detail: hata.Message, statusCode: StatusCodes.Status404NotFound);
        }
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<BirimYaniti>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<BirimYaniti>>> Listele([FromQuery] Guid? kurumId, CancellationToken iptalToken)
    {
        return Ok(await _birimServisi.ListeleAsync(kurumId, iptalToken));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(BirimYaniti), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BirimYaniti>> Detay(Guid id, CancellationToken iptalToken)
    {
        try
        {
            return Ok(await _birimServisi.DetayGetirAsync(id, iptalToken));
        }
        catch (BirimBulunamadiException hata)
        {
            return Problem(title: "Birim bulunamadi", detail: hata.Message, statusCode: StatusCodes.Status404NotFound);
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = nameof(PersonelRolu.SistemYoneticisi))]
    [ProducesResponseType(typeof(BirimYaniti), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BirimYaniti>> Guncelle(Guid id, [FromBody] BirimGuncelleIstegi istek, CancellationToken iptalToken)
    {
        try
        {
            return Ok(await _birimServisi.GuncelleAsync(id, istek, iptalToken));
        }
        catch (BirimBulunamadiException hata)
        {
            return Problem(title: "Birim bulunamadi", detail: hata.Message, statusCode: StatusCodes.Status404NotFound);
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
            await _birimServisi.DurumGuncelleAsync(id, istek.AktifMi, iptalToken);
            return NoContent();
        }
        catch (BirimBulunamadiException hata)
        {
            return Problem(title: "Birim bulunamadi", detail: hata.Message, statusCode: StatusCodes.Status404NotFound);
        }
    }
}
