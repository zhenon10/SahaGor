import { ApiIstemcisi } from "./apiIstemcisi";
import type { GirisIstegi, GirisYaniti } from "../tipler/kimlik";

export const KimlikApi = {
  girisYap: (istek: GirisIstegi) =>
    ApiIstemcisi.post<GirisYaniti>("/api/auth/login", istek, { kimlikDogrulamaGerekmez: true }),
};
