import { cookies } from "next/headers";
import { NextResponse } from "next/server";
import { backendIstemcisiHatasindanYanitUret } from "@/lib/backendHataYaniti";
import { backendIstegi } from "@/lib/backendIstemcisi";
import { YENILEME_TOKENI_COOKIE_ADI, yenilemeTokeniCookieSecenekleri } from "@/lib/kimlikCookie";
import type { GirisYaniti, OturumBilgisi } from "@/tipler/kimlik";

/**
 * Sayfa yenilendiginde (F5) veya erisim tokeni suresi dolmadan once cagrilir. httpOnly
 * cookie'deki yenileme tokeniyle sessizce yeni bir erisim tokeni alir ve cookie'yi
 * DONDURULEN yeni yenileme tokeniyle degistirir (backend'in rotasyon davranisi - bkz.
 * KimlikDogrulamaServisi.TokenYenileAsync - burada da aynen izlenir).
 */
export async function GET() {
  const cookieDeposu = await cookies();
  const yenilemeTokeni = cookieDeposu.get(YENILEME_TOKENI_COOKIE_ADI)?.value;

  if (!yenilemeTokeni) {
    return NextResponse.json({ mesaj: "Aktif oturum bulunamadi." }, { status: 401 });
  }

  try {
    const yanit = await backendIstegi<GirisYaniti>("/api/auth/refresh", {
      method: "POST",
      body: JSON.stringify({ yenilemeTokeni }),
    });

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
    // Yenileme tokeni de gecersizse artik kurtaracak bir sey yok; cerezi temizleyip
    // istemcinin tekrar giris yapmasini saglamak gerekir.
    cookieDeposu.delete(YENILEME_TOKENI_COOKIE_ADI);
    return backendIstemcisiHatasindanYanitUret(hata);
  }
}
