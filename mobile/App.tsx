import { StatusBar } from "expo-status-bar";
import { SQLiteProvider } from "expo-sqlite";
import { SafeAreaProvider } from "react-native-safe-area-context";
import { KimlikSaglayici } from "./src/baglam/KimlikBaglami";
import { veritabaniSemasiniOlustur } from "./src/depolama/veritabaniSemasi";
import { KokNavigasyon } from "./src/navigasyon/KokNavigasyon";

export default function App() {
  return (
    <SafeAreaProvider>
      <SQLiteProvider databaseName="sahagor.db" onInit={veritabaniSemasiniOlustur}>
        <KimlikSaglayici>
          <KokNavigasyon />
        </KimlikSaglayici>
      </SQLiteProvider>
      <StatusBar style="light" />
    </SafeAreaProvider>
  );
}
