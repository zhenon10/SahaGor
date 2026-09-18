import { ApiIstemcisi } from "./apiIstemcisi";

export const EkiplerApi = {
  konumGuncelle: (ekipId: string, enlem: number, boylam: number) =>
    ApiIstemcisi.post(`/api/ekipler/${ekipId}/konum`, { enlem, boylam }),
};
