"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useAuth } from "@/context/AuthContext";
import { usePermissions } from "@/context/PermissionsContext";
import { Permission, PLATFORM_ADMINISTER } from "@/lib/permissions";
import {
  getOrganizationAccess,
  isReachableWithoutOrganization,
} from "@/lib/organizationAccess";
import { useId, useState } from "react";
import { cn } from "@/lib/utils";
import { Mark } from "@/components/Mark";
import { useTranslation } from "@/context/LocalizationContext";
import { usePendingApprovalCount } from "@/lib/useApprovals";
import { useOverdueFilingCount } from "@/lib/useSuspiciousActivityReports";
import { FILINGS_HREF } from "@/lib/suspiciousActivityReports";
import {
  LayoutDashboard,
  FolderOpen,
  Inbox,
  Settings,
  Building2,
  Users,
  ChevronDown,
  ChevronRight,
  Receipt,
  Shield,
  Play,
  Users2,
  Siren,
  Layers,
  Gauge,
  SlidersHorizontal,
  Hand,
  Send,
  ShieldCheck,
  UserCheck,
  FileText,
  Tags,
  Landmark,
  ArrowLeftRight,
  Coins,
  Wrench,
  TrendingUp,
} from "lucide-react";

interface NavEntry {
  href: string;
  labelKey: string;
  icon: React.ElementType;
  exact?: boolean;
  permission?: Permission;
  anyPermission?: Permission[];
}

const navItems: NavEntry[] = [
  { href: "/", labelKey: "nav.dashboard", icon: LayoutDashboard, exact: true },
  { href: "/alerts", labelKey: "nav.alerts", icon: Siren, permission: "alerts:read" },
  {
    href: "/rule-sets",
    labelKey: "nav.ruleSets",
    icon: Shield,
    permission: "rule-sets:read",
  },
  {
    href: "/evaluate",
    labelKey: "nav.evaluate",
    icon: Play,
    anyPermission: ["rules:evaluate", "rule-sets:evaluate"],
  },
  {
    href: "/transactions",
    labelKey: "nav.transactions",
    icon: Receipt,
    permission: "transactions:read",
  },
  { href: "/parties", labelKey: "nav.parties", icon: Users2, permission: "parties:read" },
  { href: "/organization", labelKey: "nav.myOrganization", icon: Building2 },
];

const casesSubItems: NavEntry[] = [
  { href: "/cases", labelKey: "nav.cases.all", icon: FolderOpen, permission: "cases:read" },
  { href: "/my-cases", labelKey: "nav.cases.mine", icon: Inbox, permission: "cases:read" },
];

const queuesSubItems: NavEntry[] = [
  {
    href: "/queues",
    labelKey: "nav.queues.mine",
    icon: Hand,
    exact: true,
    permission: "queues:claim",
  },
  {
    href: "/queues/dashboard",
    labelKey: "nav.queues.dashboard",
    icon: Gauge,
    permission: "queues:read",
  },
  {
    href: "/queues/manage",
    labelKey: "nav.queues.configure",
    icon: SlidersHorizontal,
    permission: "queues:read",
  },
];

const approvalsSubItems: NavEntry[] = [
  {
    href: "/approvals",
    labelKey: "nav.approvals.forReview",
    icon: UserCheck,
    exact: true,
    permission: "approvals:read",
  },
  {
    href: "/approvals/mine",
    labelKey: "nav.approvals.mine",
    icon: Send,
    permission: "approvals:read",
  },
];

const adminSubItems: NavEntry[] = [
  { href: "/admin/organizations", labelKey: "nav.admin.organizations", icon: Building2 },
  { href: "/admin/users", labelKey: "nav.admin.users", icon: Users },
  {
    href: "/admin/incidents",
    labelKey: "nav.admin.incidents",
    icon: Siren,
    permission: "incidents:read",
  },
  {
    href: "/admin/maintenance-windows",
    labelKey: "nav.admin.maintenance",
    icon: Wrench,
    permission: "maintenance-windows:read",
  },
  { href: "/admin/plans", labelKey: "nav.admin.plans", icon: Tags },
  { href: "/admin/billing-entities", labelKey: "nav.admin.billingEntities", icon: Landmark },
  { href: "/admin/exchange-rates", labelKey: "nav.admin.exchangeRates", icon: ArrowLeftRight },
  {
    href: "/admin/reconciliation",
    labelKey: "nav.admin.reconciliation",
    icon: Coins,
    permission: "payments:reconcile",
  },
  {
    href: "/admin/revenue",
    labelKey: "nav.admin.revenue",
    icon: TrendingUp,
    permission: "revenue:read",
  },
];

function NavItem({
  href,
  labelKey,
  icon: Icon,
  active,
  badge,
}: {
  href: string;
  labelKey: string;
  icon: React.ElementType;
  active: boolean;
  badge?: React.ReactNode;
}) {
  const t = useTranslation();

  return (
    <Link
      href={href}
      aria-current={active ? "page" : undefined}
      className={cn(
        "flex items-center gap-3 border-l-2 px-3 py-2 font-mono text-[13px] transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-brand-600",
        active
          ? "border-brand-600 bg-white/5 text-white"
          : "border-transparent text-console-400 hover:bg-white/5 hover:text-white"
      )}
    >
      <Icon aria-hidden className="size-4 flex-shrink-0" />
      {t(labelKey)}
      {badge && <span className="ml-auto">{badge}</span>}
    </Link>
  );
}

function CountBadge({
  count,
  tone = "attention",
  title,
}: {
  count: number | null;
  tone?: "attention" | "critical";
  title: (count: number) => string;
}) {
  if (!count) return null;

  return (
    <span
      className={cn(
        "rounded-full px-1.5 py-0.5 font-mono text-[10px] font-medium leading-none",
        tone === "critical"
          ? "bg-critical-solid text-critical-solid-foreground"
          : "bg-attention-500 text-white"
      )}
      title={title(count)}
    >
      <span aria-hidden>{count > 99 ? "99+" : count}</span>
      <span className="sr-only">{title(count)}</span>
    </span>
  );
}

function GroupToggle({
  labelKey,
  icon: Icon,
  open,
  active,
  badge,
  controls,
  onToggle,
}: {
  labelKey: string;
  icon: React.ElementType;
  open: boolean;
  active: boolean;
  badge?: React.ReactNode;
  controls: string;
  onToggle: () => void;
}) {
  const t = useTranslation();

  return (
    <button
      type="button"
      onClick={onToggle}
      aria-expanded={open}
      aria-controls={controls}
      className={cn(
        "flex w-full items-center justify-between border-l-2 border-transparent px-3 py-2 font-mono text-[13px] transition-colors hover:bg-white/5 hover:text-white focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-brand-600",
        active ? "text-white" : "text-console-400"
      )}
    >
      <span className="flex items-center gap-3">
        <Icon aria-hidden className="size-4 flex-shrink-0" />
        {t(labelKey)}
      </span>
      <span className="flex items-center gap-2">
        {badge}
        {open ? (
          <ChevronDown aria-hidden className="size-3.5" />
        ) : (
          <ChevronRight aria-hidden className="size-3.5" />
        )}
      </span>
    </button>
  );
}

export default function Sidebar() {
  const t = useTranslation();
  const pathname = usePathname();
  const groupId = useId();
  const { user } = useAuth();
  const { holds, holdsAny } = usePermissions();

  const canAdministerPlatform = holds(PLATFORM_ADMINISTER);

  const organizationBlocked = getOrganizationAccess(user) !== "granted";

  const visible = (entry: NavEntry) =>
    (!organizationBlocked ||
      isReachableWithoutOrganization(entry.href, canAdministerPlatform)) &&
    (entry.anyPermission
      ? holdsAny(...entry.anyPermission)
      : !entry.permission || holds(entry.permission));

  const queuesItems = queuesSubItems.filter(visible);
  const casesItems = casesSubItems.filter(visible);
  const approvalsItems = approvalsSubItems.filter(visible);
  const adminItems = adminSubItems.filter(visible);
  const [casesOpen, setCasesOpen] = useState(
    pathname.startsWith("/cases") || pathname.startsWith("/my-cases")
  );
  const [queuesOpen, setQueuesOpen] = useState(pathname.startsWith("/queues"));
  const [approvalsOpen, setApprovalsOpen] = useState(
    pathname.startsWith("/approvals")
  );
  const [adminOpen, setAdminOpen] = useState(pathname.startsWith("/admin"));

  const pendingApprovals = usePendingApprovalCount(
    Boolean(user) && !organizationBlocked && holds("approvals:read")
  );

  const overdueFilings = useOverdueFilingCount(
    Boolean(user) &&
      !organizationBlocked &&
      holds("suspicious-activity-reports:read")
  );

  const isActive = (href: string, exact = false) =>
    exact ? pathname === href : pathname.startsWith(href);

  const casesActive =
    pathname.startsWith("/cases") || pathname.startsWith("/my-cases");

  return (
    <aside
      aria-label={t("nav.landmark.sidebar")}
      className="flex w-60 flex-shrink-0 flex-col bg-console"
    >
      <Link
        href="/"
        className="flex items-center gap-2.5 border-b border-white/10 px-5 py-4 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-brand-600"
      >
        <Mark aria-hidden className="size-4 flex-shrink-0 text-brand-600" />
        <span className="font-mono text-sm font-medium tracking-tight text-white">
          {t("app.name")}
        </span>
      </Link>

      <nav
        aria-label={t("nav.landmark.primary")}
        className="flex-1 space-y-0.5 overflow-y-auto py-3"
      >
        {navItems
          .filter(visible)
          .map((item) => (
            <NavItem
              key={item.href}
              href={item.href}
              labelKey={item.labelKey}
              icon={item.icon}
              active={isActive(item.href, item.exact)}
            />
          ))}

        {queuesItems.length > 0 && (
          <div>
            <GroupToggle
              labelKey="nav.queues"
              controls={`${groupId}-queues`}
              icon={Layers}
              open={queuesOpen}
              active={pathname.startsWith("/queues")}
              onToggle={() => setQueuesOpen((open) => !open)}
            />
            {queuesOpen && (
              <div id={`${groupId}-queues`} className="ml-4 mt-0.5 space-y-0.5">
                {queuesItems.map((item) => (
                  <NavItem
                    key={item.href}
                    href={item.href}
                    labelKey={item.labelKey}
                    icon={item.icon}
                    // `/queues` is exact, or it would light up on the two pages
                    // beneath it as well as its own.
                    active={isActive(item.href, item.exact)}
                  />
                ))}
              </div>
            )}
          </div>
        )}

        {casesItems.length > 0 && (
          <div>
            <GroupToggle
              labelKey="nav.cases"
              controls={`${groupId}-cases`}
              icon={FolderOpen}
              open={casesOpen}
              active={casesActive}
              onToggle={() => setCasesOpen((open) => !open)}
            />
            {casesOpen && (
              <div id={`${groupId}-cases`} className="ml-4 mt-0.5 space-y-0.5">
                {casesItems.map((item) => (
                  <NavItem
                    key={item.href}
                    href={item.href}
                    labelKey={item.labelKey}
                    icon={item.icon}
                    active={isActive(item.href)}
                  />
                ))}
              </div>
            )}
          </div>
        )}

        {approvalsItems.length > 0 && (
          <div>
            <GroupToggle
              labelKey="nav.approvals"
              controls={`${groupId}-approvals`}
              icon={ShieldCheck}
              open={approvalsOpen}
              active={pathname.startsWith("/approvals")}
              badge={
                <CountBadge
                  count={pendingApprovals}
                  title={(count) =>
                    t(
                      count === 1
                        ? "nav.badge.approvalWaiting"
                        : "nav.badge.approvalsWaiting",
                      count
                    )
                  }
                />
              }
              onToggle={() => setApprovalsOpen((open) => !open)}
            />
            {approvalsOpen && (
              <div id={`${groupId}-approvals`} className="ml-4 mt-0.5 space-y-0.5">
                {approvalsItems.map((item) => (
                  <NavItem
                    key={item.href}
                    href={item.href}
                    labelKey={item.labelKey}
                    icon={item.icon}
                    // `/approvals` is exact, or it would light up on `mine` and on
                    // every detail page as well as its own.
                    active={isActive(item.href, item.exact)}
                  />
                ))}
              </div>
            )}
          </div>
        )}

        {!organizationBlocked && holds("suspicious-activity-reports:read") && (
          <NavItem
            href={FILINGS_HREF}
            labelKey="nav.filings"
            icon={FileText}
            active={pathname.startsWith(FILINGS_HREF)}
            badge={
              <CountBadge
                count={overdueFilings}
                tone="critical"
                title={(count) =>
                  t(
                    count === 1
                      ? "nav.badge.filingOverdue"
                      : "nav.badge.filingsOverdue",
                    count
                  )
                }
              />
            }
          />
        )}

        {canAdministerPlatform && (
          <div>
            <GroupToggle
              labelKey="nav.admin"
              controls={`${groupId}-admin`}
              icon={Settings}
              open={adminOpen}
              active={pathname.startsWith("/admin")}
              onToggle={() => setAdminOpen((open) => !open)}
            />
            {adminOpen && (
              <div id={`${groupId}-admin`} className="ml-4 mt-0.5 space-y-0.5">
                {adminItems.map((item) => (
                  <NavItem
                    key={item.href}
                    href={item.href}
                    labelKey={item.labelKey}
                    icon={item.icon}
                    active={isActive(item.href)}
                  />
                ))}
              </div>
            )}
          </div>
        )}
      </nav>

      {user?.organization && (
        <div className="border-t border-white/10 px-5 py-3">
          <p className="font-mono text-[10px] uppercase tracking-widest text-console-600">
            {t("nav.organization.eyebrow")}
          </p>
          <p className="truncate font-mono text-xs text-console-400">
            {user.organization.friendlyName}
          </p>
        </div>
      )}

      <div className="flex flex-col gap-2 border-t border-white/10 px-5 py-3">
        <Link
          href="/status"
          className="font-mono text-[11px] uppercase tracking-widest text-console-500 transition-colors hover:text-console-300 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-600"
        >
          {t("nav.statusPage")}
        </Link>
        <Link
          href="/docs"
          className="font-mono text-[11px] uppercase tracking-widest text-console-500 transition-colors hover:text-console-300 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-600"
        >
          {t("nav.apiDocs")}
        </Link>
      </div>
    </aside>
  );
}
