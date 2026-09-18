using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SahaGor.Application.Atama;
using SahaGor.Application.Atama.Dtolar;
using SahaGor.Application.Ekipler.Istisnalar;
using SahaGor.Application.Gorevler;
using SahaGor.Application.Gorevler.Dtolar;
using SahaGor.Application.Gorevler.Istisnalar;
using SahaGor.Application.Personeller.Istisnalar;
using SahaGor.Domain.Enumlar;

namespace SahaGor.Api.Controllers;

/// <summary>GorevTalebi CRUD ve coğrafi sorgu uc noktalari (SG-130, SG-131).</summary>
[ApiController]
[Authorize]
[Route("api/gorevler")]
public class GorevlerController : ControllerBase
{
    private const string AtamaYonetimRolleri = $"{nameof(PersonelRolu.Amir)},{nameof(PersonelRolu.SistemYoneticisi)}";

    private readonly IGorevTalebiServisi _gorevTalebiServisi;
    private readonly IAtamaMotoru _atamaMotoru;
    private readonly ILogger<GorevlerController> _logger;

    public GorevlerController(IGorevTalebiServisi gorevTalebiServisi, IAtamaMotoru atamaMotoru,
        ILogger<GorevlerController> logger)
    {
        _gorevTalebiServisi = gorevTalebiServisi;
        _atamaMotoru = atamaMotoru;
        _logger = logger;
    }

    /// <summary>Yeni bir gorev talebi olusturur; konumu kapsayan bolge varsa otomatik atanir.</summary>
    [HttpPost]
    [Authorize(Roles = $"{nameof(PersonelRolu.Operator)},{nameof(PersonelRolu.Amir)},{nameof(PersonelRolu.SistemYoneticisi)}")]
    [ProducesResponseType(typeof(GorevTalebiDetayYaniti), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<GorevTalebiDetayYaniti>> Olustur([FromBody] GorevTalebiOlusturIstegi istek,
        CancellationToken iptalToken)
    {
        try
        {
            var yanit = await _gorevTalebiServisi.OlusturAsync(istek, GecerliPersonelId(), iptalToken);
            return CreatedAtAction(nameof(Detay), new { id = yanit.Id }, yanit);
        }
        catch (GorevKategorisiBulunamadiException hata)
        {
            return Problem(title: "Kategori bulunamadi", detail: hata.Message, statusCode: StatusCodes.Status404NotFound);
        }
        catch (GorevKategorisiPasifException hata)
        {
            return Problem(title: "Kategori pasif", detail: hata.Message, statusCode: StatusCodes.Status409Conflict);
        }
    }

    /// <summary>Durum, kategori, bolge ve tarih araligina gore filtrelenmis, sayfalanmis gorev listesi.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(SayfalanmisSonuc<GorevTalebiOzetYaniti>), StatusCodes.Status200OK)]
    public async Task<ActionResult<SayfalanmisSonuc<GorevTalebiOzetYaniti>>> Listele(
        [FromQuery] GorevDurumu? durum,
        [FromQuery] Guid? kategoriId,
        [FromQuery] Guid? bolgeId,
        [FromQuery] Guid? atananEkipId,
        [FromQuery] Guid? atananPersonelId,
        [FromQuery] DateTime? baslangicTarihiUtc,
        [FromQuery] DateTime? bitisTarihiUtc,
        [FromQuery] int sayfa = 1,
        [FromQuery] int sayfaBoyutu = 20,
        CancellationToken iptalToken = default)
    {
        var filtre = new GorevTalebiFiltre
        {
            Durum = durum,
            KategoriId = kategoriId,
            BolgeId = bolgeId,
            AtananEkipId = atananEkipId,
            AtananPersonelId = atananPersonelId,
            BaslangicTarihiUtc = baslangicTarihiUtc,
            BitisTarihiUtc = bitisTarihiUtc,
            Sayfa = sayfa,
            SayfaBoyutu = sayfaBoyutu,
        };

        var sonuc = await _gorevTalebiServisi.ListeleAsync(filtre, iptalToken);
        return Ok(sonuc);
    }

    /// <summary>Verilen konuma belirtilen yaricap icinde kalan, hala acik olan gorevleri mesafeye gore siralar (SG-131).</summary>
    [HttpGet("yakinimdaki")]
    [ProducesResponseType(typeof(IReadOnlyList<YakinimdakiGorevYaniti>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<YakinimdakiGorevYaniti>>> Yakinimdaki(
        [FromQuery] double lat,
        [FromQuery] double lon,
        [FromQuery] double radius = 500,
        [FromQuery] int maksimumSonuc = 50,
        CancellationToken iptalToken = default)
    {
        var sonuc = await _gorevTalebiServisi.YakinimdakiGetirAsync(lat, lon, radius, maksimumSonuc, iptalToken);
        return Ok(sonuc);
    }

    /// <summary>Komuta paneli KPI dashboard'u icin ozet istatistikler (SG-321).</summary>
    [HttpGet("istatistikler")]
    [ProducesResponseType(typeof(GorevIstatistikleriYaniti), StatusCodes.Status200OK)]
    public async Task<ActionResult<GorevIstatistikleriYaniti>> Istatistikler([FromQuery] int gunSayisi = 7,
        CancellationToken iptalToken = default)
    {
        return Ok(await _gorevTalebiServisi.IstatistikleriGetirAsync(gunSayisi, iptalToken));
    }

    /// <summary>Fotograflar ve durum gecmisi dahil, tek bir gorevin tam detayi.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(GorevTalebiDetayYaniti), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GorevTalebiDetayYaniti>> Detay(Guid id, CancellationToken iptalToken)
    {
        try
        {
            return Ok(await _gorevTalebiServisi.DetayGetirAsync(id, iptalToken));
        }
        catch (GorevTalebiBulunamadiException hata)
        {
            return Problem(title: "Gorev bulunamadi", detail: hata.Message, statusCode: StatusCodes.Status404NotFound);
        }
    }

    /// <summary>
    /// Gorev icin atama motorunun urettigi onerilen ekip ve tum adaylarin skor dokumu
    /// (SG-310, SG-312). Sistem burada HICBIR ATAMA YAPMAZ; sadece oneriyi doner.
    /// </summary>
    [HttpGet("{id:guid}/atama-onerisi")]
    [Authorize(Roles = AtamaYonetimRolleri)]
    [ProducesResponseType(typeof(AtamaOnerisiYaniti), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AtamaOnerisiYaniti>> AtamaOnerisi(Guid id, CancellationToken iptalToken)
    {
        try
        {
            return Ok(await _atamaMotoru.OneriHesaplaAsync(id, iptalToken));
        }
        catch (GorevTalebiBulunamadiException hata)
        {
            return Problem(title: "Gorev bulunamadi", detail: hata.Message, statusCode: StatusCodes.Status404NotFound);
        }
    }

    /// <summary>
    /// Amir, atama motorunun onerisini tek tikla onaylayabilir (govdede onerilen ekipId'yi
    /// gonderir) veya tamamen farkli bir ekip secebilir (SG-311) - her iki durumda da bu
    /// tek uc nokta kullanilir.
    /// </summary>
    [HttpPost("{id:guid}/ata")]
    [Authorize(Roles = AtamaYonetimRolleri)]
    [ProducesResponseType(typeof(GorevTalebiDetayYaniti), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<GorevTalebiDetayYaniti>> Ata(Guid id, [FromBody] GorevAtaIstegi istek,
        CancellationToken iptalToken)
    {
        try
        {
            var yanit = await _gorevTalebiServisi.AtaAsync(id, istek.EkipId, istek.SorumluPersonelId,
                GecerliPersonelId()!.Value, iptalToken);
            return Ok(yanit);
        }
        catch (GorevTalebiBulunamadiException hata)
        {
            return Problem(title: "Gorev bulunamadi", detail: hata.Message, statusCode: StatusCodes.Status404NotFound);
        }
        catch (EkipBulunamadiException hata)
        {
            return Problem(title: "Ekip bulunamadi", detail: hata.Message, statusCode: StatusCodes.Status404NotFound);
        }
        catch (PersonelBulunamadiException hata)
        {
            return Problem(title: "Personel bulunamadi", detail: hata.Message, statusCode: StatusCodes.Status404NotFound);
        }
        catch (GorevDurumCakismasiException hata)
        {
            return DurumCakismasiYaniti(hata);
        }
    }

    /// <summary>
    /// Amir, saha personelinin "Tamamlandi" olarak isaretledigi bir gorevi is kalitesi
    /// acisindan inceleyip onaylar (SG-402). Onaydan sonra, gorevi bildiren bir vatandas
    /// telefon numarasi birakmissa kendisine bilgilendirme SMS'i gonderilir.
    /// </summary>
    [HttpPost("{id:guid}/dogrula")]
    [Authorize(Roles = AtamaYonetimRolleri)]
    [ProducesResponseType(typeof(GorevTalebiDetayYaniti), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<GorevTalebiDetayYaniti>> Dogrula(Guid id, CancellationToken iptalToken)
    {
        try
        {
            var yanit = await _gorevTalebiServisi.DogrulaAsync(id, GecerliPersonelId()!.Value, iptalToken);
            return Ok(yanit);
        }
        catch (GorevTalebiBulunamadiException hata)
        {
            return Problem(title: "Gorev bulunamadi", detail: hata.Message, statusCode: StatusCodes.Status404NotFound);
        }
        catch (GorevDurumCakismasiException hata)
        {
            return DurumCakismasiYaniti(hata);
        }
    }

    /// <summary>
    /// Amir, incelemede eksik/hatali buldugu bir gorevi gerekcesiyle birlikte sahaya
    /// (saha personeline) geri gonderir; gorev tekrar "Devam Ediyor" durumuna doner (SG-402).
    /// </summary>
    [HttpPost("{id:guid}/geri-gonder")]
    [Authorize(Roles = AtamaYonetimRolleri)]
    [ProducesResponseType(typeof(GorevTalebiDetayYaniti), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<GorevTalebiDetayYaniti>> SahayaGeriGonder(Guid id,
        [FromBody] GorevGeriGonderIstegi istek, CancellationToken iptalToken)
    {
        try
        {
            var yanit = await _gorevTalebiServisi.SahayaGeriGonderAsync(id, GecerliPersonelId()!.Value, istek.Neden,
                iptalToken);
            return Ok(yanit);
        }
        catch (GorevTalebiBulunamadiException hata)
        {
            return Problem(title: "Gorev bulunamadi", detail: hata.Message, statusCode: StatusCodes.Status404NotFound);
        }
        catch (GorevDurumCakismasiException hata)
        {
            return DurumCakismasiYaniti(hata);
        }
    }

    /// <summary>Bir gorevi gerekcesiyle birlikte iptal eder (mukerrer kayit, yanlis ihbar vb.).</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = $"{nameof(PersonelRolu.Amir)},{nameof(PersonelRolu.SistemYoneticisi)}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> IptalEt(Guid id, [FromBody] GorevIptalIstegi istek, CancellationToken iptalToken)
    {
        try
        {
            await _gorevTalebiServisi.IptalEtAsync(id, istek.Neden, GecerliPersonelId()!.Value, iptalToken);
            return NoContent();
        }
        catch (GorevTalebiBulunamadiException hata)
        {
            return Problem(title: "Gorev bulunamadi", detail: hata.Message, statusCode: StatusCodes.Status404NotFound);
        }
        catch (InvalidOperationException hata)
        {
            return Problem(title: "Islem gecersiz", detail: hata.Message, statusCode: StatusCodes.Status409Conflict);
        }
    }

    /// <summary>Saha personeli gorev konumuna dogru yola cikinca cagirir (SG-220).</summary>
    [HttpPost("{id:guid}/yola-cik")]
    [Authorize(Roles = nameof(PersonelRolu.SahaPersoneli))]
    [ProducesResponseType(typeof(GorevTalebiDetayYaniti), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<GorevTalebiDetayYaniti>> YolaCik(Guid id, CancellationToken iptalToken)
    {
        try
        {
            return Ok(await _gorevTalebiServisi.YolaCikAsync(id, GecerliPersonelId()!.Value, iptalToken));
        }
        catch (GorevTalebiBulunamadiException hata)
        {
            return Problem(title: "Gorev bulunamadi", detail: hata.Message, statusCode: StatusCodes.Status404NotFound);
        }
        catch (GorevErisimYetkisiYokException hata)
        {
            return Problem(title: "Erisim yetkisi yok", detail: hata.Message, statusCode: StatusCodes.Status403Forbidden);
        }
        catch (GorevDurumCakismasiException hata)
        {
            return DurumCakismasiYaniti(hata);
        }
    }

    /// <summary>Saha personeli sahada islemeye baslayinca cagirir (SG-220).</summary>
    [HttpPost("{id:guid}/basla")]
    [Authorize(Roles = nameof(PersonelRolu.SahaPersoneli))]
    [ProducesResponseType(typeof(GorevTalebiDetayYaniti), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<GorevTalebiDetayYaniti>> Basla(Guid id, CancellationToken iptalToken)
    {
        try
        {
            return Ok(await _gorevTalebiServisi.BaslatAsync(id, GecerliPersonelId()!.Value, iptalToken));
        }
        catch (GorevTalebiBulunamadiException hata)
        {
            return Problem(title: "Gorev bulunamadi", detail: hata.Message, statusCode: StatusCodes.Status404NotFound);
        }
        catch (GorevErisimYetkisiYokException hata)
        {
            return Problem(title: "Erisim yetkisi yok", detail: hata.Message, statusCode: StatusCodes.Status403Forbidden);
        }
        catch (GorevDurumCakismasiException hata)
        {
            return DurumCakismasiYaniti(hata);
        }
    }

    /// <summary>Offline cekilip bekletilen bir kanit fotografini yukler (SG-212).</summary>
    [HttpPost("{id:guid}/fotograflar")]
    [Authorize(Roles = nameof(PersonelRolu.SahaPersoneli))]
    [RequestSizeLimit(10_000_000)]
    [ProducesResponseType(typeof(GorevFotografiYaniti), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GorevFotografiYaniti>> FotografEkle(Guid id, [FromForm] GorevFotografiYukleFormu form,
        CancellationToken iptalToken)
    {
        if (form.Dosya.Length == 0)
        {
            return Problem(title: "Dosya bos", detail: "Yuklenen fotograf dosyasi bos olamaz.", statusCode: StatusCodes.Status400BadRequest);
        }

        if (form.Dosya.ContentType is not ("image/jpeg" or "image/png"))
        {
            return Problem(title: "Gecersiz dosya turu", detail: "Sadece JPEG veya PNG fotograflar kabul edilir.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var uzanti = form.Dosya.ContentType == "image/png" ? ".png" : ".jpg";

        try
        {
            await using var akis = form.Dosya.OpenReadStream();
            var yanit = await _gorevTalebiServisi.FotografEkleAsync(id, GecerliPersonelId()!.Value, akis, uzanti,
                form.Asama, form.Enlem, form.Boylam, form.CekilmeZamaniUtc, iptalToken);

            return CreatedAtAction(nameof(Detay), new { id }, yanit);
        }
        catch (GorevTalebiBulunamadiException hata)
        {
            return Problem(title: "Gorev bulunamadi", detail: hata.Message, statusCode: StatusCodes.Status404NotFound);
        }
        catch (GorevErisimYetkisiYokException hata)
        {
            return Problem(title: "Erisim yetkisi yok", detail: hata.Message, statusCode: StatusCodes.Status403Forbidden);
        }
        catch (InvalidOperationException hata)
        {
            return Problem(title: "Gecersiz cekim zamani", detail: hata.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    /// <summary>
    /// SG-203 kabul kriteri: cakisma durumunda istemciye 409 + sunucudaki guncel durum bilgisi
    /// dondurulur; mobil senkron motoru bu bilgiyle yerel onbellegini duzeltip kuyruktaki
    /// gecersiz hale gelmis islemi atar.
    /// </summary>
    private ObjectResult DurumCakismasiYaniti(GorevDurumCakismasiException hata)
    {
        var problemDetaylari = new ProblemDetails
        {
            Title = "Gorev durumu degisti",
            Detail = hata.Message,
            Status = StatusCodes.Status409Conflict,
        };
        problemDetaylari.Extensions["gorevId"] = hata.GorevId;
        problemDetaylari.Extensions["mevcutDurum"] = hata.MevcutDurum;

        return Conflict(problemDetaylari);
    }

    private Guid? GecerliPersonelId()
    {
        var deger = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(deger, out var id) ? id : null;
    }
}

/// <summary>DELETE /api/gorevler/{id} istegi govdesi.</summary>
public sealed record GorevIptalIstegi(string Neden);

/// <summary>POST /api/gorevler/{id}/ata istegi govdesi.</summary>
public sealed record GorevAtaIstegi(Guid EkipId, Guid? SorumluPersonelId);

/// <summary>POST /api/gorevler/{id}/geri-gonder istegi govdesi.</summary>
public sealed record GorevGeriGonderIstegi(string Neden);

/// <summary>
/// POST /api/gorevler/{id}/fotograflar multipart/form-data istegi. IFormFile'i diger
/// [FromForm] alanlariyla ayni action imzasinda kullanmak Swashbuckle'da bir sema
/// uretim hatasina yol acar; tumunu tek bir model uzerinde toplamak hem Swagger'i hem
/// model binding'i duzgun calistiran onerilen (Swashbuckle dokumantasyonundaki) yontemdir.
/// Model binding'in parametresiz kurucu + set edilebilir ozellik beklemesi nedeniyle
/// bilerek "record" degil siradan bir sinif olarak tanimlanir.
/// </summary>
public sealed class GorevFotografiYukleFormu
{
    public IFormFile Dosya { get; set; } = null!;
    public GorevFotografAsamasi Asama { get; set; }
    public double Enlem { get; set; }
    public double Boylam { get; set; }
    public DateTime CekilmeZamaniUtc { get; set; }
}
