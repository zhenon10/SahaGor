using System.ComponentModel.DataAnnotations;

namespace SahaGor.Application.Ekipler.Dtolar;

public sealed record UyePersonelIstegi([property: Required] Guid PersonelId);

public sealed record UzmanlikKategorisiIstegi([property: Required] Guid GorevKategorisiId);
