import { cookies } from "next/headers";
import { NextResponse } from "next/server";
import { YENILEME_TOKENI_COOKIE_ADI } from "@/lib/kimlikCookie";

export async function POST() {
  const cookieDeposu = await cookies();
  cookieDeposu.delete(YENILEME_TOKENI_COOKIE_ADI);
  return new NextResponse(null, { status: 204 });
}
