import * as signalR from "@microsoft/signalr";
import { ErisimTokeniDeposu } from "./erisimTokeniDeposu";
import { Ortam } from "./ortam";

/**
 * Komuta panelinin canli bildirim baglantisini kurar (SG-140, SG-301, SG-302). Hub, Api
 * katmaninda "[Authorize(Roles = Amir,SistemYoneticisi)]" ile korunur; bu yuzden token
 * gerekir. "accessTokenFactory" her (yeniden) baglanti denemesinde CAGRILIR (bir kez
 * degil) - boylece erisim tokeni yenilendikten sonraki otomatik yeniden baglanmalarda
 * da guncel token kullanilir.
 */
export function signalRBaglantisiOlustur(): signalR.HubConnection {
  return new signalR.HubConnectionBuilder()
    .withUrl(`${Ortam.apiTabanAdresi}/hub/gorevler`, {
      accessTokenFactory: () => ErisimTokeniDeposu.getir() ?? "",
    })
    .withAutomaticReconnect()
    .configureLogging(signalR.LogLevel.Warning)
    .build();
}
