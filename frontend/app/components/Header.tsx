"use client";

import { useRouter } from "next/navigation";
import { useAuth } from "@/context/AuthContext";
import { Avatar, AvatarFallback } from "@/components/ui/avatar";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { ThemeToggle } from "@/components/ThemeToggle";
import { EnvironmentSwitcher } from "@/components/EnvironmentSwitcher";
import { useTranslation } from "@/context/LocalizationContext";

const roleLabel = (roles: string[] | undefined): string | null => {
  if (!roles?.length) return null;
  return roles.join(" · ").toLowerCase();
};

export default function Header() {
  const t = useTranslation();
  const { user } = useAuth();
  const router = useRouter();
  const initials = user?.email?.slice(0, 2).toUpperCase() ?? "??";

  return (
    <header
      aria-label={t("nav.landmark.account")}
      className="flex h-14 flex-shrink-0 items-center justify-end gap-4 border-b border-slate-200 bg-card px-4 sm:px-6"
    >
      <EnvironmentSwitcher />
      <ThemeToggle />
      {user && (
        <>
          <div className="hidden min-w-0 text-right sm:block">
            <p className="truncate font-mono text-xs text-slate-600">
              {user.email}
            </p>
            {roleLabel(user.roles) && (
              <p className="font-mono text-[10px] uppercase tracking-widest text-slate-400">
                {roleLabel(user.roles)}
              </p>
            )}
          </div>

          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <button
                type="button"
                aria-label={t("header.menu.open")}
                aria-haspopup="menu"
                className="rounded-full focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-800 focus-visible:ring-offset-2"
              >
                <Avatar aria-hidden className="h-8 w-8 cursor-pointer">
                  <AvatarFallback className="bg-brand-800 font-mono text-xs font-medium text-white">
                    {initials}
                  </AvatarFallback>
                </Avatar>
              </button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end" className="w-44">
              <DropdownMenuItem onClick={() => router.push("/profile")}>
                {t("header.menu.profile")}
              </DropdownMenuItem>
              <DropdownMenuSeparator />
              <DropdownMenuItem
                className="text-critical-700 focus:text-critical-700"
                onClick={() => router.push("/auth/logout")}
              >
                {t("header.menu.logOut")}
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        </>
      )}
    </header>
  );
}
