import { GuvenliDepolama } from "../depolama/guvenliDepolama";
import { ApiHatasi, OturumSuresiDolduHatasi, type ProblemDetaylari } from "../tipler/api";
import type { GirisYaniti } from "../tipler/kimlik";
import { Ortam } from "../yapilandirma/ortam";

interface IstekSecenekleri {
  /** Bu istek icin Authorization header'i eklenmesin mi (orn. giris/yenile uc noktalari). */
  kimlikDogrulamaGerekmez?: boolean;
}

/**
 * Tum backend cagrilarinin gectigi tek nokta. Sorumluluklari:
 *  - Taban adresi ve JSON header'larini merkezi olarak eklemek,
 *  - Guvenli depolamadaki erisim tokenini otomatik eklemek,
 *  - 401 alindiginda BIR KEZ yenileme tokeniyle sessizce token yenileyip istegi tekrarlamak
 *    (SG-122: saha personeli oturumu, senkronizasyon sirasinda kesintiye ugramamali),
 *  - Basarisiz yanitlari ProblemDetails'ten okunabilir bir hataya cevirmek.
 */
async function istekYap<TYanit>(yol: string, init: RequestInit = {}, secenekler: IstekSecenekleri = {}): Promise<TYanit> {
  const yaniti = await hamIstekYap(yol, init, secenekler);

  if (yaniti.status === 401 && !secenekler.kimlikDogrulamaGerekmez) {
    const yenilendiMi = await erisimTokeniniYenile();

    if (!yenilendiMi) {
      throw new OturumSuresiDolduHatasi();
    }

    const tekrarYaniti = await hamIstekYap(yol, init, secenekler);
    return yanitiIsle<TYanit>(tekrarYaniti);
  }

  return yanitiIsle<TYanit>(yaniti);
}

async function hamIstekYap(yol: string, init: RequestInit, secenekler: IstekSecenekleri): Promise<Response> {
  const headerlar = new Headers(init.headers);

  // FormData (dosya yuklemede kullanilir) icin Content-Type BILEREK ayarlanmaz;
  // fetch/RN, sinir (boundary) degerini icerecek dogru "multipart/form-data; boundary=..."
  // header'ini otomatik uretir. Burada elle "application/json" atansaydi sunucu
  // govdeyi parcalayamaz ve istek 400/415 ile basarisiz olurdu.
  if (!(init.body instanceof FormData)) {
    headerlar.set("Content-Type", "application/json");
  }

  if (!secenekler.kimlikDogrulamaGerekmez) {
    const erisimTokeni = await GuvenliDepolama.erisimTokeniGetir();
    if (erisimTokeni) {
      headerlar.set("Authorization", `Bearer ${erisimTokeni}`);
    }
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

/** Yenileme tokeni ile sessizce yeni bir erisim tokeni alir. Basarisizsa false doner (tekrar giris gerekir). */
async function erisimTokeniniYenile(): Promise<boolean> {
  const yenilemeTokeni = await GuvenliDepolama.yenilemeTokeniGetir();
  if (!yenilemeTokeni) {
    return false;
  }

  try {
    const yaniti = await hamIstekYap(
      "/api/auth/refresh",
      { method: "POST", body: JSON.stringify({ yenilemeTokeni }) },
      { kimlikDogrulamaGerekmez: true },
    );

    if (!yaniti.ok) {
      return false;
    }

    const yeniJetonlar = (await yaniti.json()) as GirisYaniti;
    await GuvenliDepolama.tokenCiftiKaydet(yeniJetonlar.erisimTokeni, yeniJetonlar.yenilemeTokeni);
    return true;
  } catch {
    return false;
  }
}

export const ApiIstemcisi = {
  get: <TYanit>(yol: string, secenekler?: IstekSecenekleri) =>
    istekYap<TYanit>(yol, { method: "GET" }, secenekler),

  post: <TYanit>(yol: string, govde: unknown, secenekler?: IstekSecenekleri) =>
    istekYap<TYanit>(yol, { method: "POST", body: JSON.stringify(govde) }, secenekler),

  /** Dosya yuklemek icin multipart/form-data istegi (bkz. hamIstekYap'taki Content-Type notu). */
  postForm: <TYanit>(yol: string, formVerisi: FormData, secenekler?: IstekSecenekleri) =>
    istekYap<TYanit>(yol, { method: "POST", body: formVerisi }, secenekler),

  put: <TYanit>(yol: string, govde: unknown, secenekler?: IstekSecenekleri) =>
    istekYap<TYanit>(yol, { method: "PUT", body: JSON.stringify(govde) }, secenekler),

  patch: <TYanit>(yol: string, govde: unknown, secenekler?: IstekSecenekleri) =>
    istekYap<TYanit>(yol, { method: "PATCH", body: JSON.stringify(govde) }, secenekler),

  delete: <TYanit>(yol: string, govde?: unknown, secenekler?: IstekSecenekleri) =>
    istekYap<TYanit>(yol, { method: "DELETE", body: govde ? JSON.stringify(govde) : undefined }, secenekler),
};
