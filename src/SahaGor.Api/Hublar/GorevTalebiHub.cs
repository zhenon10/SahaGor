using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SahaGor.Domain.Enumlar;

namespace SahaGor.Api.Hublar;

/// <summary>
/// Komuta paneli (Amir) istemcilerinin baglandigi SignalR hub'i. Su an sadece grup uyeligi
/// yonetimini saglar; canli harita UI'i Sprint 3'te bu hub'a abone olacaktir (SG-140).
/// </summary>
[Authorize(Roles = $"{nameof(PersonelRolu.Amir)},{nameof(PersonelRolu.SistemYoneticisi)}")]
public class GorevTalebiHub : Hub<IGorevTalebiHubClient>
{
    /// <summary>Tum gorev bildirimlerinin yayinlandigi grup adi.</summary>
    public const string AmirPaneliGrubu = "amir-paneli";

    /// <summary>Baglanan istemci, canli bildirimleri almak icin bu metodu cagirarak gruba katilir.</summary>
    public async Task AmirPanelineKatil()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, AmirPaneliGrubu);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, AmirPaneliGrubu);
        await base.OnDisconnectedAsync(exception);
    }
}
