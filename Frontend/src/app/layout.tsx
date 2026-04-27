import type { Metadata } from "next";
import "./globals.css";

export const metadata: Metadata = {
  title:       "معافى+ | لوحة تحكم الطبيب",
  description: "منصة توليد محتوى تعليمي طبي مخصص للمرضى بعد التشخيص — اليمن",
};

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="ar" dir="rtl">
      <head>
        <link rel="preconnect" href="https://fonts.googleapis.com" />
        <link rel="preconnect" href="https://fonts.gstatic.com" crossOrigin="anonymous" />
        <link
          href="https://fonts.googleapis.com/css2?family=IBM+Plex+Sans+Arabic:wght@400;500;600;700&family=Noto+Sans+Arabic:wght@400;500;600;700&display=swap"
          rel="stylesheet"
        />
      </head>
      <body className="font-arabic bg-gray-50 text-gray-900 antialiased">
        {children}
      </body>
    </html>
  );
}
