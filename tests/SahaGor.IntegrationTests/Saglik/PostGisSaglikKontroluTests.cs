using Microsoft.Extensions.Diagnostics.HealthChecks;
using SahaGor.Infrastructure.Saglik;
using SahaGor.IntegrationTests.Gorevler;

namespace SahaGor.IntegrationTests.Saglik;

/// <summary>
/// SG-142/SG-420: PostGisSaglikKontrolu, EF Core'un skaler SqlQuery&lt;T&gt; API'sini kullanir;
/// bu API sonuc kolonunun tam olarak "Value" adinda olmasini bekler. Bu test, gercek bir
/// PostGIS'e karsi calisip calismadigini dogrular - InMemory saglayicisi SqlQuery'i
/// desteklemedigi/hic calistirmadigi icin bu tur bir hata birim testlerinde ASLA yakalanamaz.
/// </summary>
[Collection(PostGisTestCollection.Adi)]
public class PostGisSaglikKontroluTests
{
    private readonly PostGisTestFixture _fixture;

    public PostGisSaglikKontroluTests(PostGisTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CheckHealthAsync_Gercek_PostGIS_Baglantisinda_Healthy_Doner()
    {
        await using var dbContext = _fixture.DbContextOlustur();
        var saglikKontrolu = new PostGisSaglikKontrolu(dbContext);

        var sonuc = await saglikKontrolu.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Healthy, sonuc.Status);
        Assert.NotNull(sonuc.Data);
        Assert.True(sonuc.Data.ContainsKey("postgisSurumu"));
        Assert.NotEqual("bilinmiyor", sonuc.Data["postgisSurumu"]);
    }
}
