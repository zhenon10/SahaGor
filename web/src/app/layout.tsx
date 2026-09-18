import type { Metadata } from "next";
import { KimlikSaglayici } from "@/baglam/KimlikBaglami";
import "./globals.css";

export const metadata: Metadata = {
  title: "SahaGör Komuta Paneli",
  description: "Belediye saha ekiplerinin canlı görev takibi ve yönetim paneli.",
};

export default function RootLayout({ children }: LayoutProps<"/">) {
  return (
    <html lang="tr" className="h-full">
      <body className="min-h-full antialiased">
        <KimlikSaglayici>{children}</KimlikSaglayici>
      </body>
    </html>
  );
}
