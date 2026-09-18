import "server-only";
import { NextResponse } from "next/server";
import { ApiHatasi } from "@/tipler/api";

/** Route Handler'larda tekrar eden "backend hatasini JSON yanita cevir" mantigini tek yerde toplar. */
export function backendIstemcisiHatasindanYanitUret(hata: unknown): NextResponse {
  if (hata instanceof ApiHatasi) {
    return NextResponse.json({ mesaj: hata.message }, { status: hata.durumKodu });
  }

  return NextResponse.json({ mesaj: "Backend sunucusuna ulasilamadi." }, { status: 502 });
}
