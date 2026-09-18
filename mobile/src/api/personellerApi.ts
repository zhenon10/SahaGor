import { ApiIstemcisi } from "./apiIstemcisi";
import type { PersonelBilgisi } from "../tipler/personel";

export const PersonellerApi = {
  /** Giris sirasinda donen jetonlarda yer almayan EkipId gibi bilgiler icin cagirilir. */
  benimBilgilerimGetir: (personelId: string) => ApiIstemcisi.get<PersonelBilgisi>(`/api/personeller/${personelId}`),
};
