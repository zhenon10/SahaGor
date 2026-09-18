import "server-only";
import { ApiHatasi, type ProblemDetaylari } from "@/tipler/api";
import { Ortam } from "./ortam";

/**
 * SADECE Route Handler'lar icinde kullanilir: Next.js sunucusundan .NET backend'ine
 * yapilan sunucu-sunucu cagrilari (login/refresh). "server-only" paketi, bu dosyanin
 * yanlislikla bir Client Component'e import edilmesini derleme zamaninda engeller.
 */
export async function backendIstegi<TYanit>(yol: string, init: RequestInit = {}): Promise<TYanit> {
  const headerlar = new Headers(init.headers);
  headerlar.set("Content-Type", "application/json");

  const yaniti = await fetch(`${Ortam.apiTabanAdresi}${yol}`, { ...init, headers: headerlar });

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
