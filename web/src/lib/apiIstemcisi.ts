import { ErisimTokeniDeposu } from "./erisimTokeniDeposu";
import { Ortam } from "./ortam";
import { ApiHatasi, type ProblemDetaylari } from "@/tipler/api";
import type { OturumBilgisi } from "@/tipler/kimlik";

/**
 * Tarayicidan .NET backend'ine DOGRUDAN yapilan cagrilarin gectigi tek nokta (harita
 * verisi, gorev/ekip yonetimi vb. - Sprint 3'un sonraki adimlarinda kullanilacak).
 * 401 alindiginda, mobildeki ile ayni felsefeyle, ayni Next.js sunucusundaki
 * "/api/auth/session" ucuna basvurup (o da httpOnly cookie'deki yenileme tokenini
 * kullanir) BIR KEZ sessizce token yenileyip istegi tekrarlar.
 */
async function hamIstekYap(yol: string, init: RequestInit): Promise<Response> {
  const headerlar = new Headers(init.headers);
  headerlar.set("Content-Type", "application/json");

  const erisimTokeni = ErisimTokeniDeposu.getir();
  if (erisimTokeni) {
    headerlar.set("Authorization", `Bearer ${erisimTokeni}`);
  }

  return fetch(`${Ortam.apiTabanAdresi}${yol}`, { ...init, headers: headerlar });
}

async function yanitiIsle<TYanit>(yaniti: Response): Promise<TYanit> {
  if (yaniti.status === 204) {
    return undefined as TYanit;
  }

  const govdeMetni = await yaniti.text();
  const govde = govdeMetni ? JSON.parse(govdeMetni) : undefined;

  if (!yaniti.ok) {
    const problemDetaylari = govde as ProblemDetaylari | undefined;
    const mesaj = problemDetaylari?.detail ?? problemDetaylari?.title ?? "Bilinmeyen bir hata olustu.";
    throw new ApiHatasi(mesaj, yaniti.status, problemDetaylari);
  }

  return govde as TYanit;
}

async function erisimTokeniniYenile(): Promise<boolean> {
  try {
    const yaniti = await fetch("/api/auth/session");
    if (!yaniti.ok) {
      return false;
    }

    const oturum = (await yaniti.json()) as OturumBilgisi;
    ErisimTokeniDeposu.ayarla(oturum.erisimTokeni);
    return true;
  } catch {
    return false;
  }
}

async function istekYap<TYanit>(yol: string, init: RequestInit = {}): Promise<TYanit> {
  const yaniti = await hamIstekYap(yol, init);

  if (yaniti.status === 401) {
    const yenilendiMi = await erisimTokeniniYenile();
    if (!yenilendiMi) {
      throw new ApiHatasi("Oturumunuzun suresi doldu. Lutfen tekrar giris yapin.", 401);
    }

    return yanitiIsle<TYanit>(await hamIstekYap(yol, init));
  }

  return yanitiIsle<TYanit>(yaniti);
}

export const ApiIstemcisi = {
  get: <TYanit>(yol: string) => istekYap<TYanit>(yol, { method: "GET" }),

  post: <TYanit>(yol: string, govde: unknown) =>
    istekYap<TYanit>(yol, { method: "POST", body: JSON.stringify(govde) }),

  put: <TYanit>(yol: string, govde: unknown) =>
    istekYap<TYanit>(yol, { method: "PUT", body: JSON.stringify(govde) }),

  patch: <TYanit>(yol: string, govde: unknown) =>
    istekYap<TYanit>(yol, { method: "PATCH", body: JSON.stringify(govde) }),

  delete: <TYanit>(yol: string, govde?: unknown) =>
    istekYap<TYanit>(yol, { method: "DELETE", body: govde ? JSON.stringify(govde) : undefined }),
};
