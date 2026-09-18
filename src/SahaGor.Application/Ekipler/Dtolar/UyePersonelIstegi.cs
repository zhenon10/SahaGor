using System.ComponentModel.DataAnnotations;

namespace SahaGor.Application.Ekipler.Dtolar;

public sealed record UyePersonelIstegi([Required] Guid PersonelId);

public sealed record UzmanlikKategorisiIstegi([Required] Guid GorevKategorisiId);
