import { AuthProvider } from "@/context/AuthContext";
import { PermissionsProvider } from "@/context/PermissionsContext";
import { ModeProvider } from "@/context/ModeContext";
import { LocalizationProvider } from "@/context/LocalizationContext";
import AppShell from "@/components/AppShell";
import RateLimitBanner from "@/components/RateLimitBanner";

export default function ConsoleLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <>
      <RateLimitBanner />
      <AuthProvider>
        <PermissionsProvider>
          <LocalizationProvider>
            <ModeProvider>
              <AppShell>{children}</AppShell>
            </ModeProvider>
          </LocalizationProvider>
        </PermissionsProvider>
      </AuthProvider>
    </>
  );
}
