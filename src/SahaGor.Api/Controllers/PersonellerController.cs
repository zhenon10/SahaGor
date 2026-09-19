using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SahaGor.Application.Birimler.Istisnalar;
using SahaGor.Application.Personeller;
using SahaGor.Application.Personeller.Dtolar;
using SahaGor.Application.Personeller.Istisnalar;
using SahaGor.Domain.Enumlar;

namespace SahaGor.Api.Controllers;

/// <summary>
/// Personel yonetimi CRUD uc noktalari (SG-110). Yazma islemlerinin tumu ve tam liste
/// Amir/SistemYoneticisi ile sinirlidir; tek istisna, herhangi bir kullanicinin SADECE
/// KENDI kaydini goruntuleyebilmesidir (bkz. Detay).
/// </summary>
[ApiController]
[Authorize]
[Route("api/personeller")]
public class PersonellerController : ControllerBase
{
    private const string YonetimRolleri = $"{nameof(PersonelRolu.Amir)},{nameof(PersonelRolu.SistemYoneticisi)}";

    private readonly IPersonelServisi _personelServisi;

    public PersonellerController(IPersonelServisi personelServisi)
    {
        _personelServisi = personelServisi;
    }

    [HttpPost]
    [Authorize(Roles = YonetimRolleri)]
    [ProducesResponseType(typeof(PersonelYaniti), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PersonelYaniti>> Olustur([FromBody] PersonelOlusturIstegi istek, CancellationToken iptalToken)
    {
        try
        {
            var yanit = await _personelServisi.OlusturAsync(istek, iptalToken);
            return CreatedAtAction(nameof(Detay), new { id = yanit.Id }, yanit);
        }
        catch (BirimBulunamadiException hata)
        {
            return Problem(title: "Birim bulunamadi", detail: hata.Message, statusCode: StatusCodes.Status404NotFound);
        }
        catch (KullaniciAdiZatenKullanimdaException hata)
        {
            return Problem(title: "Kullanici adi kullanimda", detail: hata.Message, statusCode: StatusCodes.Status409Conflict);
        }
    }

    [HttpGet]
    [Authorize(Roles = YonetimRolleri)]
    [ProducesResponseType(typeof(IReadOnlyList<PersonelYaniti>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PersonelYaniti>>> Listele(
        [FromQuery] Guid? birimId,
        [FromQuery] PersonelRolu? rol,
        [FromQuery] bool? aktifMi,
        CancellationToken iptalToken)
    {
        var filtre = new PersonelFiltre { BirimId = birimId, Rol = rol, AktifMi = aktifMi };
        return Ok(await _personelServisi.ListeleAsync(filtre, iptalToken));
    }

    /// <summary>
    /// Amir/SistemYoneticisi herkesin detayini gorebilir; diger roller (orn. mobil uygulamadaki
    /// saha personeli, giris sonrasi JWT'de olmayan EkipId gibi bilgileri almak icin) SADECE
    /// KENDI kaydini gorebilir (SG-423, OWASP A01 - Broken Access Control/IDOR). Bu kisitlama
    /// olmadan herhangi bir gecerli JWT'ye sahip kullanici, ID'sini tahmin/elde ettigi HERHANGI
    /// BIR personelin telefon/e-posta/kullanici adi gibi kisisel bilgilerine erisebilirdi.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PersonelYaniti), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PersonelYaniti>> Detay(Guid id, CancellationToken iptalToken)
    {
        var yonetimRolundeMi = User.IsInRole(nameof(PersonelRolu.Amir)) || User.IsInRole(nameof(PersonelRolu.SistemYoneticisi));

        if (!yonetimRolundeMi && GecerliPersonelId() != id)
        {
            return Problem(title: "Erisim yetkisi yok", detail: "Sadece kendi personel kaydinizi goruntuleyebilirsiniz.",
                statusCode: StatusCodes.Status403Forbidden);
        }

        try
        {
            return Ok(await _personelServisi.DetayGetirAsync(id, iptalToken));
        }
        catch (PersonelBulunamadiException hata)
        {
            return Problem(title: "Personel bulunamadi", detail: hata.Message, statusCode: StatusCodes.Status404NotFound);
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = YonetimRolleri)]
    [ProducesResponseType(typeof(PersonelYaniti), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PersonelYaniti>> Guncelle(Guid id, [FromBody] PersonelGuncelleIstegi istek, CancellationToken iptalToken)
    {
        try
        {
            return Ok(await _personelServisi.GuncelleAsync(id, istek, iptalToken));
        }
        catch (PersonelBulunamadiException hata)
        {
            return Problem(title: "Personel bulunamadi", detail: hata.Message, statusCode: StatusCodes.Status404NotFound);
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
            await _personelServisi.DurumGuncelleAsync(id, istek.AktifMi, iptalToken);
            return NoContent();
        }
        catch (PersonelBulunamadiException hata)
        {
            return Problem(title: "Personel bulunamadi", detail: hata.Message, statusCode: StatusCodes.Status404NotFound);
        }
    }

    [HttpPost("{id:guid}/sifre-sifirla")]
    [Authorize(Roles = YonetimRolleri)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SifreSifirla(Guid id, [FromBody] SifreSifirlaIstegi istek, CancellationToken iptalToken)
    {
        try
        {
            await _personelServisi.SifreSifirlaAsync(id, istek, iptalToken);
            return NoContent();
        }
        catch (PersonelBulunamadiException hata)
        {
            return Problem(title: "Personel bulunamadi", detail: hata.Message, statusCode: StatusCodes.Status404NotFound);
        }
    }

    private Guid? GecerliPersonelId()
    {
        var deger = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(deger, out var id) ? id : null;
    }
}
