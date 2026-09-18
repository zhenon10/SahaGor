using Microsoft.AspNetCore.SignalR;
using SahaGor.Application.Bildirimler;

namespace SahaGor.Api.Hublar;

/// <summary>
/// IGorevBildirimYayinlayici ve IEkipBildirimYayinlayici sozlesmelerinin somut SignalR
/// implementasyonu. Bu sinifin Api katmaninda olmasinin nedeni: IHubContext, ancak ASP.NET
/// Core barindirma (hosting) ortaminda anlamlidir; Infrastructure/Application katmanlari
/// SignalR'den tamamen habersiz kalir (bagimlilik tersine cevirme - somut adaptor en
/// diste, sozlesme ic katmanda).
/// </summary>
public sealed class SignalRBildirimYayinlayici : IGorevBildirimYayinlayici, IEkipBildirimYayinlayici
{
    private readonly IHubContext<GorevTalebiHub, IGorevTalebiHubClient> _hubContext;

    public SignalRBildirimYayinlayici(IHubContext<GorevTalebiHub, IGorevTalebiHubClient> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task YayinlaAsync(GorevBildirimi bildirim, CancellationToken iptalToken = default)
    {
        return _hubContext.Clients
            .Group(GorevTalebiHub.AmirPaneliGrubu)
            .GorevGuncellendi(bildirim, iptalToken);
    }

    public Task YayinlaAsync(EkipKonumBildirimi bildirim, CancellationToken iptalToken = default)
    {
        return _hubContext.Clients
            .Group(GorevTalebiHub.AmirPaneliGrubu)
            .EkipKonumuGuncellendi(bildirim, iptalToken);
    }
}
