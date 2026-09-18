import * as Location from "expo-location";

/** Konum izni yoksa null doner; cagiran taraf kullaniciyi zorlamadan sessizce vazgecebilir. */
export async function anlikKonumAl(): Promise<{ enlem: number; boylam: number } | null> {
  const { status } = await Location.requestForegroundPermissionsAsync();
  if (status !== "granted") {
    return null;
  }

  const konum = await Location.getCurrentPositionAsync({ accuracy: Location.Accuracy.High });
  return { enlem: konum.coords.latitude, boylam: konum.coords.longitude };
}
