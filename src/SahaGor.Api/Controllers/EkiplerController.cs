using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SahaGor.Application.Birimler.Istisnalar;
using SahaGor.Application.Ekipler;
using SahaGor.Application.Ekipler.Dtolar;
using SahaGor.Application.Ekipler.Istisnalar;
using SahaGor.Application.Gorevler.Istisnalar;
using SahaGor.Application.Personeller.Istisnalar;
using SahaGor.Domain.Enumlar;

namespace SahaGor.Api.Controllers;

/// <summary>Ekip yonetimi CRUD uc noktalari ve uye/uzmanlik alani atamalari (SG-110).</summary>
[ApiController]
[Authorize]
[Route("api/ekipler")]
public class EkiplerController : ControllerBase
{
    private const string YonetimRolleri = $"{nameof(PersonelRolu.Amir)},{nameof(PersonelRolu.SistemYoneticisi)}";

    private readonly IEkipServisi _ekipServisi;

    public EkiplerController(IEkipServisi ekipServisi)
    {
        _ekipServisi = ekipServisi;
    }

    [HttpPost]
    [Authorize(Roles = YonetimRolleri)]
    [ProducesResponseType(typeof(EkipYaniti), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EkipYaniti>> Olustur([FromBody] EkipOlusturIstegi istek, CancellationToken iptalToken)
    {
        try
        {
            var yanit = await _ekipServisi.OlusturAsync(istek, iptalToken);
            return CreatedAtAction(nameof(Detay), new { id = yanit.Id }, yanit);
        }
        catch (BirimBulunamadiException hata)
        {
            return Problem(title: "Birim bulunamadi", detail: hata.Message, statusCode: StatusCodes.Status404NotFound);
        }
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<EkipYaniti>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<EkipYaniti>>> Listele([FromQuery] Guid? birimId, CancellationToken iptalToken)
    {
        return Ok(await _ekipServisi.ListeleAsync(birimId, iptalToken));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(EkipYaniti), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EkipYaniti>> Detay(Guid id, CancellationToken iptalToken)
    {
        try
        {
            return Ok(await _ekipServisi.DetayGetirAsync(id, iptalToken));
        }
        catch (EkipBulunamadiException hata)
        {
            return Problem(title: "Ekip bulunamadi", detail: hata.Message, statusCode: StatusCodes.Status404NotFound);
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = YonetimRolleri)]
    [ProducesResponseType(typeof(EkipYaniti), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EkipYaniti>> Guncelle(Guid id, [FromBody] EkipGuncelleIstegi istek, CancellationToken iptalToken)
    {
        try
        {
            return Ok(await _ekipServisi.GuncelleAsync(id, istek, iptalToken));
        }
        catch (EkipBulunamadiException hata)
        {
            return Problem(title: "Ekip bulunamadi", detail: hata.Message, statusCode: StatusCodes.Status404NotFound);
        }
    }

    [HttpPatch("{id:guid}/durum")]
    [Authorize(Roles = YonetimRolleri)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DurumGuncelle(Guid id, [FromBody] DurumGuncelleIstegi istek, CancellationToken iptalToken)
    {
        try
        {
            await _ekipServisi.DurumGuncelleAsync(id, istek.AktifMi, iptalToken);
            return NoContent();
        }
        catch (EkipBulunamadiException hata)
        {
            return Problem(title: "Ekip bulunamadi", detail: hata.Message, statusCode: StatusCodes.Status404NotFound);
        }
    }

    [HttpPost("{id:guid}/uyeler")]
    [Authorize(Roles = YonetimRolleri)]
    [ProducesResponseType(typeof(EkipYaniti), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EkipYaniti>> UyeEkle(Guid id, [FromBody] UyePersonelIstegi istek, CancellationToken iptalToken)
    {
        try
        {
            return Ok(await _ekipServisi.UyeEkleAsync(id, istek.PersonelId, iptalToken));
        }
        catch (EkipBulunamadiException hata)
        {
            return Problem(title: "Ekip bulunamadi", detail: hata.Message, statusCode: StatusCodes.Status404NotFound);
        }
        catch (PersonelBulunamadiException hata)
        {
            return Problem(title: "Personel bulunamadi", detail: hata.Message, statusCode: StatusCodes.Status404NotFound);
        }
        catch (InvalidOperationException hata)
        {
            return Problem(title: "Islem gecersiz", detail: hata.Message, statusCode: StatusCodes.Status409Conflict);
        }
    }

    [HttpDelete("{id:guid}/uyeler/{personelId:guid}")]
    [Authorize(Roles = YonetimRolleri)]
    [ProducesResponseType(typeof(EkipYaniti), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EkipYaniti>> UyeCikar(Guid id, Guid personelId, CancellationToken iptalToken)
    {
        try
        {
            return Ok(await _ekipServisi.UyeCikarAsync(id, personelId, iptalToken));
        }
        catch (EkipBulunamadiException hata)
        {
            return Problem(title: "Ekip bulunamadi", detail: hata.Message, statusCode: StatusCodes.Status404NotFound);
        }
        catch (PersonelBulunamadiException hata)
        {
            return Problem(title: "Personel bulunamadi", detail: hata.Message, statusCode: StatusCodes.Status404NotFound);
        }
    }

    [HttpPost("{id:guid}/uzmanlik-alanlari")]
    [Authorize(Roles = YonetimRolleri)]
    [ProducesResponseType(typeof(EkipYaniti), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EkipYaniti>> UzmanlikAlaniEkle(Guid id, [FromBody] UzmanlikKategorisiIstegi istek,
        CancellationToken iptalToken)
    {
        try
        {
            return Ok(await _ekipServisi.UzmanlikAlaniEkleAsync(id, istek.GorevKategorisiId, iptalToken));
        }
        catch (EkipBulunamadiException hata)
        {
            return Problem(title: "Ekip bulunamadi", detail: hata.Message, statusCode: StatusCodes.Status404NotFound);
        }
        catch (GorevKategorisiBulunamadiException hata)
        {
            return Problem(title: "Kategori bulunamadi", detail: hata.Message, statusCode: StatusCodes.Status404NotFound);
        }
    }

    [HttpDelete("{id:guid}/uzmanlik-alanlari/{kategoriId:guid}")]
    [Authorize(Roles = YonetimRolleri)]
    [ProducesResponseType(typeof(EkipYaniti), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EkipYaniti>> UzmanlikAlaniCikar(Guid id, Guid kategoriId, CancellationToken iptalToken)
    {
        try
        {
            return Ok(await _ekipServisi.UzmanlikAlaniCikarAsync(id, kategoriId, iptalToken));
        }
        catch (EkipBulunamadiException hata)
        {
            return Problem(title: "Ekip bulunamadi", detail: hata.Message, statusCode: StatusCodes.Status404NotFound);
        }
    }

    /// <summary>Mobil uygulamadan gelen GPS bildirimiyle ekibin canli konumunu gunceller (SG-301, SG-302).</summary>
    [HttpPost("{id:guid}/konum")]
    [Authorize(Roles = nameof(PersonelRolu.SahaPersoneli))]
    [ProducesResponseType(typeof(EkipYaniti), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EkipYaniti>> KonumGuncelle(Guid id, [FromBody] KonumGuncelleIstegi istek,
        CancellationToken iptalToken)
    {
        try
        {
            var yanit = await _ekipServisi.KonumGuncelleAsync(id, GecerliPersonelId()!.Value, istek.Enlem, istek.Boylam,
                iptalToken);
            return Ok(yanit);
        }
        catch (EkipBulunamadiException hata)
        {
            return Problem(title: "Ekip bulunamadi", detail: hata.Message, statusCode: StatusCodes.Status404NotFound);
        }
        catch (EkipErisimYetkisiYokException hata)
        {
            return Problem(title: "Erisim yetkisi yok", detail: hata.Message, statusCode: StatusCodes.Status403Forbidden);
        }
    }

    private Guid? GecerliPersonelId()
    {
        var deger = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(deger, out var id) ? id : null;
    }
}

/// <summary>POST /api/ekipler/{id}/konum istegi govdesi.</summary>
public sealed record KonumGuncelleIstegi(double Enlem, double Boylam);
