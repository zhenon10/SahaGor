import { EkiplerApi } from "../api/ekiplerApi";
import { anlikKonumAl } from "./konumServisi";

/**
 * Ekibin canli konumunu sunucuya bildirir (SG-301, SG-302). "Yola Cik" gibi hareketliligin
 * baslangicini isaretleyen anlarda cagrilir; surekli arka plan konum takibi bu kapsamda
 * DEGILDIR (ayri, ozel olarak tasarlanmasi gereken bir ozellik - pil tuketimi, izin
 * yonetimi acisindan). Best-effort bir bildirimdir: basarisiz olursa gorev akisini
 * engellemez, sadece sessizce yutulur.
 */
export async function ekipKonumunuBildir(ekipId: string): Promise<void> {
  try {
    const konum = await anlikKonumAl();
    if (!konum) {
      return;
    }

    await EkiplerApi.konumGuncelle(ekipId, konum.enlem, konum.boylam);
  } catch {
    // Konum bildirimi ikincil bir islevdir; hatasi kullaniciya gosterilmez.
  }
}
