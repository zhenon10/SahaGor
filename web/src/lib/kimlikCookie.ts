import "server-only";

/**
 * Yenileme tokeni SADECE bu httpOnly cookie icinde tutulur; tarayici JS'i (dolayisiyla
 * bir XSS saldirisi) bu degere asla erisemez. Erisim tokeni ise kisa omurlu oldugu icin
 * bilerek tarayicida bellekte (React context) tutulur - bkz. lib/erisimTokeniDeposu.ts.
 */
export const YENILEME_TOKENI_COOKIE_ADI = "sahagor_yenileme_tokeni";

export function yenilemeTokeniCookieSecenekleri(sonKullanmaZamaniUtc: string) {
  return {
    httpOnly: true,
    // Gelistirme ortaminda (http://localhost) "secure" cerezler bazi taraycilarda
    // reddedilebildigi icin sadece production'da (https) zorunlu kilinir.
    secure: process.env.NODE_ENV === "production",
    sameSite: "lax" as const,
    path: "/",
    expires: new Date(sonKullanmaZamaniUtc),
  };
}
