import type { SQLiteDatabase } from "expo-sqlite";
import { GorevlerApi } from "../api/gorevlerApi";
import { FotografDepolama } from "../depolama/fotografDepolama";

/**
 * Bekleyen (henuz yuklenmemis) kanit fotograflarini, cekilme sirasina gore tek tek
 * sunucuya yukler (SG-212). Bir fotograf agirlikli oldugu icin (birkac MB), her istek
 * biraz surebilir; bir tanesi ag hatasiyla basarisiz olursa FIFO sirasini korumak ve
 * gereksiz paralel yuklemeyle bant genisligini bosa harcamamak icin isleme durur.
 */
export async function bekleyenFotograflariYukle(db: SQLiteDatabase): Promise<void> {
  const fotograflar = await FotografDepolama.yuklenmemisleriGetir(db);

  for (const fotograf of fotograflar) {
    try {
      await GorevlerApi.fotografYukle(fotograf);
      await FotografDepolama.yuklendiOlarakIsaretle(db, fotograf.id);
    } catch {
      // Ag hatasi veya sunucu hatasi: kalan fotograflari bir sonraki denemeye birak.
      break;
    }
  }
}
