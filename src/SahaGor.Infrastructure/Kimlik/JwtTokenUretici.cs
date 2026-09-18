using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SahaGor.Application.Kimlik;
using SahaGor.Domain.Varliklar;

namespace SahaGor.Infrastructure.Kimlik;

/// <summary>HMAC-SHA256 ile imzalanmis JWT erisim tokenlari ve rastgele yenileme tokenlari uretir.</summary>
public sealed class JwtTokenUretici : IJwtTokenUretici
{
    private readonly JwtAyarlari _ayarlar;

    public JwtTokenUretici(IOptions<JwtAyarlari> ayarlar)
    {
        _ayarlar = ayarlar.Value;
        _ayarlar.Dogrula();
    }

    public (string Token, DateTime SonKullanmaZamaniUtc) ErisimTokeniUret(Personel personel)
    {
        ArgumentNullException.ThrowIfNull(personel);

        var sonKullanmaZamaniUtc = DateTime.UtcNow.AddMinutes(_ayarlar.ErisimTokeniDakika);

        var talepler = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, personel.Id.ToString()),
            new(ClaimTypes.NameIdentifier, personel.Id.ToString()),
            new(ClaimTypes.Name, personel.KullaniciAdi),
            new(ClaimTypes.Role, personel.Rol.ToString()),
            new("birimId", personel.BirimId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var imzalamaAnahtari = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_ayarlar.Anahtar));
        var imzalamaBilgisi = new SigningCredentials(imzalamaAnahtari, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _ayarlar.Yayinlayan,
            audience: _ayarlar.Kitle,
            claims: talepler,
            expires: sonKullanmaZamaniUtc,
            signingCredentials: imzalamaBilgisi);

        var tokenDegeri = new JwtSecurityTokenHandler().WriteToken(token);
        return (tokenDegeri, sonKullanmaZamaniUtc);
    }

    public string YenilemeTokeniUret()
    {
        // 64 bayt (512 bit) kriptografik olarak guvenli rastgele deger; tahmin edilmesi
        // pratikte imkansizdir. Base64Url kullanilir ki URL/JSON icinde sorunsuz tasinsin.
        var rastgeleBaytlar = RandomNumberGenerator.GetBytes(64);
        return Base64UrlEncoder.Encode(rastgeleBaytlar);
    }

    public DateTime YenilemeTokeniSonKullanmaZamaniHesapla() =>
        DateTime.UtcNow.AddDays(_ayarlar.YenilemeTokeniGunSayisi);
}
