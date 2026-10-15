"use client";

import { useEffect } from "react";
import { usePathname, useRouter } from "next/navigation";
import { toast } from "sonner";
import Sidebar from "./Sidebar";
import Header from "./Header";
import OrganizationGate from "./OrganizationGate";
import { useAuth } from "@/context/AuthContext";
import { usePermissions } from "@/context/PermissionsContext";
import { useEnvironment } from "@/context/ModeContext";
import {
  getOrganizationAccess,
  isReachableWithoutOrganization,
  OrganizationAccess,
} from "@/lib/organizationAccess";
import { currentLocation, loginHref } from "@/lib/authRedirect";
import { ACCEPT_INVITATION_HREF } from "@/lib/invitations";
import { useTranslation } from "@/context/LocalizationContext";
import { ApplicationUser } from "@/types";
import { PLATFORM_ADMINISTER } from "@/lib/permissions";

const isPublicRoute = (pathname: string) =>
  pathname === "/" ||
  pathname.startsWith("/auth") ||
  pathname.startsWith(ACCEPT_INVITATION_HREF);

const isStandaloneRoute = (pathname: string) =>
  pathname.startsWith("/auth") || pathname.startsWith(ACCEPT_INVITATION_HREF);

const ADMIN_HOME_HREF = "/admin/organizations";

const blockingAccess = (
  user: ApplicationUser | null,
  pathname: string,
  canAdministerPlatform: boolean
): Exclude<OrganizationAccess, "granted"> | null => {
  if (!user || isStandaloneRoute(pathname)) return null;

  const access = getOrganizationAccess(user);
  if (access === "granted") return null;

  return isReachableWithoutOrganization(pathname, canAdministerPlatform)
    ? null
    : access;
};

export default function AppShell({
  children,
}: {
  children: React.ReactNode;
}) {
  const t = useTranslation();
  const pathname = usePathname();
  const router = useRouter();
  const { user, authResolved } = useAuth();
  const { holds, resolved: permissionsResolved } = usePermissions();
  const { mode, ready: environmentReady, environmentEpoch } = useEnvironment();

  const isPublic = isPublicRoute(pathname);
  const needsSignIn = authResolved && !user && !isPublic;
  // A null user before the check answers only means "not known yet", so hold
  // the page back instead of mounting it against one.
  const awaitingSession = !authResolved && !isPublic;

  const canAdministerPlatform = holds(PLATFORM_ADMINISTER);
  const blocked = blockingAccess(user, pathname, canAdministerPlatform);
  const redirectingAdmin = blocked !== null && canAdministerPlatform;
  const awaitingPermissions = blocked !== null && !permissionsResolved;

  useEffect(() => {
    if (!redirectingAdmin) return;
    router.replace(ADMIN_HOME_HREF);
  }, [redirectingAdmin, router]);

  useEffect(() => {
    if (!needsSignIn) return;
    toast.error(t("common.pleaseLoginAccessResource"), { id: "unauthorized" });
    router.replace(loginHref(currentLocation()));
  }, [needsSignIn, router, t]);

  if (
    needsSignIn ||
    awaitingSession ||
    !environmentReady ||
    awaitingPermissions ||
    redirectingAdmin
  ) {
    return (
      <div className="flex h-screen items-center justify-center bg-slate-50">
        <div className="size-12 animate-spin rounded-full border-2 border-brand-600 border-t-transparent" />
      </div>
    );
  }

  if (isStandaloneRoute(pathname) || !user) {
    return <>{children}</>;
  }

  if (blocked) {
    return <OrganizationGate access={blocked} />;
  }

  return (
    <EnvironmentScopedShell key={`${mode}:${environmentEpoch}`}>
      {children}
    </EnvironmentScopedShell>
  );
}

function EnvironmentScopedShell({ children }: { children: React.ReactNode }) {
  const t = useTranslation();

  return (
    <div className="flex h-screen overflow-hidden bg-slate-50">
      <a
        href="#main"
        className="sr-only z-50 focus:not-sr-only focus:fixed focus:left-4 focus:top-4 focus:rounded-md focus:bg-primary focus:px-4 focus:py-2 focus:font-mono focus:text-sm focus:text-primary-foreground focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2"
      >
        {t("nav.skipToContent")}
      </a>
      <Sidebar />
      <div className="flex min-w-0 flex-1 flex-col overflow-hidden">
        <Header />
        {/* `tabIndex={-1}` so the skip link above can actually move focus here.
            A bare `href="#main"` scrolls the region into view but leaves focus
            on the link, so the next Tab goes back into the nav — which makes
            the skip link look like it did nothing. */}
        <main
          id="main"
          tabIndex={-1}
          className="relative flex-1 overflow-auto p-6 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-ring"
        >
          {children}
        </main>
      </div>
    </div>
  );
}
