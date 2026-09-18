using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SahaGor.Infrastructure.Persistence;

namespace SahaGor.Api.Controllers;

/// <summary>
/// Gorev kategorilerinin salt-okunur listesi. GorevTalebi olusturma formunda kategori
/// secimi icin kullanilir; tam yonetim (CRUD) ekrani ileriki bir sprintte eklenecektir.
/// </summary>
[ApiController]
[Authorize]
[Route("api/gorev-kategorileri")]
public class GorevKategorileriController : ControllerBase
{
    private readonly SahaGorDbContext _dbContext;

    public GorevKategorileriController(SahaGorDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> Listele(CancellationToken iptalToken)
    {
        var kategoriler = await _dbContext.GorevKategorileri
            .Where(k => k.AktifMi)
            .OrderBy(k => k.Ad)
            .Select(k => new
            {
                k.Id,
                k.Ad,
                k.Aciklama,
                k.SlaYanitSuresiDakika,
                k.SlaCozumSuresiDakika,
            })
            .ToListAsync(iptalToken);

        return Ok(kategoriler);
    }
}
