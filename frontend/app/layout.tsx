import type { Metadata } from "next";
import localFont from "next/font/local";
import "@/globals.css";
import { ThemeProvider, THEME_STORAGE_KEY } from "@/context/ThemeContext";
import { ThemedToaster } from "@/components/ThemedToaster";

/**
 * Runs before first paint, so a dark-mode user never sees a white flash.
 * It has to be inline and synchronous — anything deferred lands after the
 * browser has already painted the default theme. Deliberately duplicates the
 * resolve logic in `ThemeContext`; the provider reconciles to whatever this
 * put on the element.
 */
const noFlashScript = `
(function () {
  try {
    var stored = localStorage.getItem(${JSON.stringify(THEME_STORAGE_KEY)});
    var dark = stored === "dark" || ((!stored || stored === "system") &&
      window.matchMedia("(prefers-color-scheme: dark)").matches);
    if (dark) document.documentElement.classList.add("dark");
    document.documentElement.style.colorScheme = dark ? "dark" : "light";
  } catch (e) {}
})();
`;

const geistSans = localFont({
  src: "./fonts/GeistVF.woff",
  variable: "--font-geist-sans",
  weight: "100 900",
});
const geistMono = localFont({
  src: "./fonts/GeistMonoVF.woff",
  variable: "--font-geist-mono",
  weight: "100 900",
});

export const metadata: Metadata = {
  title: "Transaction Monitoring",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    // The pre-paint script mutates <html> before React hydrates, which is
    // exactly the mismatch this suppresses.
    <html lang="en" suppressHydrationWarning>
      <head>
        <script dangerouslySetInnerHTML={{ __html: noFlashScript }} />
      </head>
      <body className={`${geistSans.variable} ${geistMono.variable} antialiased`}>
        <ThemeProvider>
          <ThemedToaster />
          {children}
        </ThemeProvider>
      </body>
    </html>
  );
}
