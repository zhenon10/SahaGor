import { cookies } from "next/headers";
import { NextResponse } from "next/server";
import { backendIstemcisiHatasindanYanitUret } from "@/lib/backendHataYaniti";
import { backendIstegi } from "@/lib/backendIstemcisi";
import { YENILEME_TOKENI_COOKIE_ADI, yenilemeTokeniCookieSecenekleri } from "@/lib/kimlikCookie";
import type { GirisIstegi, GirisYaniti, OturumBilgisi } from "@/tipler/kimlik";

/**
 * Tarayicinin dogrudan degil, bu Route Handler uzerinden giris yapmasinin tek nedeni:
 * yenileme tokenini httpOnly cookie'ye yazabilmek. Backend'in kendisi cookie kavramini
 * bilmez, sadece JSON govdede iki token doner; bu ayrim burada yapilir.
 */
export async function POST(request: Request) {
  const istek = (await request.json()) as GirisIstegi;

  try {
    const yanit = await backendIstegi<GirisYaniti>("/api/auth/login", {
      method: "POST",
      body: JSON.stringify(istek),
    });

    const cookieDeposu = await cookies();
    cookieDeposu.set(
      YENILEME_TOKENI_COOKIE_ADI,
      yanit.yenilemeTokeni,
      yenilemeTokeniCookieSecenekleri(yanit.yenilemeTokeniSonKullanmaZamaniUtc),
    );

    const oturum: OturumBilgisi = {
      erisimTokeni: yanit.erisimTokeni,
      erisimTokeniSonKullanmaZamaniUtc: yanit.erisimTokeniSonKullanmaZamaniUtc,
      personelId: yanit.personelId,
      adSoyad: yanit.adSoyad,
      rol: yanit.rol,
    };

    return NextResponse.json(oturum);
  } catch (hata) {
    return backendIstemcisiHatasindanYanitUret(hata);
  }
}
