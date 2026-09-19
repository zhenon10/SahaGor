using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using SahaGor.Api.Controllers;
using SahaGor.Application.Personeller;
using SahaGor.Application.Personeller.Dtolar;
using SahaGor.Domain.Enumlar;

namespace SahaGor.IntegrationTests.Personeller;

/// <summary>
/// SG-423 (OWASP A01 - Broken Access Control / IDOR) kabul kriterini dogrular: yonetim
/// rolunde olmayan bir kullanici SADECE kendi personel kaydini goruntuleyebilmeli, baskasinin
/// ID'sini vererek erisemenelidir.
/// </summary>
public class PersonellerControllerTests
{
    private sealed class SahtePersonelServisi : IPersonelServisi
    {
        public Task<PersonelYaniti> OlusturAsync(PersonelOlusturIstegi istek, CancellationToken iptalToken = default) =>
            throw new NotImplementedException();

        public Task<IReadOnlyList<PersonelYaniti>> ListeleAsync(PersonelFiltre filtre, CancellationToken iptalToken = default) =>
            throw new NotImplementedException();

        public Task<PersonelYaniti> DetayGetirAsync(Guid id, CancellationToken iptalToken = default) =>
            Task.FromResult(new PersonelYaniti(id, Guid.NewGuid(), "Fen Isleri Mudurlugu", null, null,
                "Test Personel", "test.personel", "5551112233", null, nameof(PersonelRolu.SahaPersoneli),
                true, true, false));

        public Task<PersonelYaniti> GuncelleAsync(Guid id, PersonelGuncelleIstegi istek, CancellationToken iptalToken = default) =>
            throw new NotImplementedException();

        public Task DurumGuncelleAsync(Guid id, bool aktifMi, CancellationToken iptalToken = default) =>
            throw new NotImplementedException();

        public Task SifreSifirlaAsync(Guid id, SifreSifirlaIstegi istek, CancellationToken iptalToken = default) =>
            throw new NotImplementedException();
    }

    /// <summary>
    /// Controller'in "Problem()" yardimcisi, HttpContext.RequestServices uzerinden
    /// ProblemDetailsFactory arar; bu yuzden minimal bir MVC servis saglayicisi kurulur.
    /// </summary>
    private static IServiceProvider ProblemDetailsServisSaglayicisiOlustur()
    {
        var servisler = new ServiceCollection();
        servisler.AddLogging();
        servisler.AddControllers();
        return servisler.BuildServiceProvider();
    }

    private static PersonellerController ControllerOlustur(Guid oturumAcanPersonelId, PersonelRolu rol)
    {
        var controller = new PersonellerController(new SahtePersonelServisi());

        var kimlik = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, oturumAcanPersonelId.ToString()),
            new Claim(ClaimTypes.Role, rol.ToString()),
        }, authenticationType: "Test");

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(kimlik),
                RequestServices = ProblemDetailsServisSaglayicisiOlustur(),
            },
        };

        return controller;
    }

    [Fact]
    public async Task Detay_SahaPersoneli_Kendi_Kaydini_Isteyince_200_Doner()
    {
        var kendiId = Guid.NewGuid();
        var controller = ControllerOlustur(kendiId, PersonelRolu.SahaPersoneli);

        var sonuc = await controller.Detay(kendiId, CancellationToken.None);

        var okSonucu = Assert.IsType<OkObjectResult>(sonuc.Result);
        Assert.Equal(StatusCodes.Status200OK, okSonucu.StatusCode);
    }

    [Fact]
    public async Task Detay_SahaPersoneli_Baskasinin_Kaydini_Isteyince_403_Doner()
    {
        var kendiId = Guid.NewGuid();
        var baskasininId = Guid.NewGuid();
        var controller = ControllerOlustur(kendiId, PersonelRolu.SahaPersoneli);

        var sonuc = await controller.Detay(baskasininId, CancellationToken.None);

        var problemSonucu = Assert.IsType<ObjectResult>(sonuc.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, problemSonucu.StatusCode);
    }

    [Fact]
    public async Task Detay_Amir_Baskasinin_Kaydini_Isteyince_200_Doner()
    {
        var amirId = Guid.NewGuid();
        var baskasininId = Guid.NewGuid();
        var controller = ControllerOlustur(amirId, PersonelRolu.Amir);

        var sonuc = await controller.Detay(baskasininId, CancellationToken.None);

        var okSonucu = Assert.IsType<OkObjectResult>(sonuc.Result);
        Assert.Equal(StatusCodes.Status200OK, okSonucu.StatusCode);
    }
}
