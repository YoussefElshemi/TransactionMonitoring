"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { toast } from "sonner";
import {
  Check,
  Copy,
  CreditCard,
  Hourglass,
  LogOut,
  PowerOff,
  RefreshCw,
} from "lucide-react";

import { useAuth } from "@/context/AuthContext";
import { Button } from "@/components/ui/button";
import {
  getOrganizationAccess,
  OrganizationAccess,
} from "@/lib/organizationAccess";
import { useTranslation } from "@/context/LocalizationContext";

const POLL_INTERVAL_MS = 15_000;

const copy: Record<
  Exclude<OrganizationAccess, "granted">,
  {
    icon: typeof Hourglass;
    eyebrowKey: string;
    titleKey: string;
    bodyKey: string;
  }
> = {
  pending: {
    icon: Hourglass,
    eyebrowKey: "organizationGate.waitingForAccess",
    titleKey: "organizationGate.notInOrganizationYet",
    bodyKey: "organizationGate.notInOrganizationYetBody",
  },
  overdue: {
    icon: CreditCard,
    eyebrowKey: "organizationGate.accessPaused",
    titleKey: "organizationGate.unpaidInvoice",
    bodyKey: "organizationGate.unpaidInvoiceBody",
  },
  disabled: {
    icon: PowerOff,
    eyebrowKey: "organizationGate.accessPaused",
    titleKey: "organizationGate.organizationSwitchedOff",
    bodyKey: "organizationGate.organizationSwitchedOffBody",
  },
};

const stillBlocked: Record<Exclude<OrganizationAccess, "granted">, string> = {
  pending: "organizationGate.stillWaiting",
  overdue: "organizationGate.stillPausedInvoice",
  disabled: "organizationGate.stillPausedDisabled",
};

function CopyableField({ label, value }: { label: string; value: string }) {
  const t = useTranslation();
  const [copied, setCopied] = useState(false);

  const handleCopy = async () => {
    try {
      await navigator.clipboard.writeText(value);
      setCopied(true);
      setTimeout(() => setCopied(false), 1500);
    } catch {
      toast.error(t("common.copyingIsnTAvailableBrowser"));
    }
  };

  return (
    <div className="flex items-center justify-between gap-4 px-4 py-3">
      <div className="min-w-0">
        <p className="text-xs font-medium uppercase tracking-wide text-slate-400">
          {label}
        </p>
        <p className="truncate font-mono text-sm text-slate-700">{value}</p>
      </div>
      <button
        type="button"
        onClick={handleCopy}
        aria-label={`Copy ${label.toLowerCase()}`}
        className="flex-shrink-0 rounded-md border border-slate-200 p-1.5 text-slate-400 transition-colors hover:border-slate-300 hover:text-slate-600 focus:outline-none focus:ring-2 focus:ring-brand-800 focus:ring-offset-2"
      >
        {copied ? (
          <Check className="size-3.5 text-settled-700" />
        ) : (
          <Copy className="size-3.5" />
        )}
      </button>
    </div>
  );
}

export default function OrganizationGate({
  access,
}: {
  access: Exclude<OrganizationAccess, "granted">;
}) {
  const t = useTranslation();
  const { user, refreshUser } = useAuth();
  const [checking, setChecking] = useState(false);
  const { icon: Icon, eyebrowKey, titleKey, bodyKey } = copy[access];

  const check = async (announce: boolean) => {
    setChecking(true);
    try {
      const refreshed = await refreshUser();
      if (announce && getOrganizationAccess(refreshed) !== "granted") {
        toast.info(t(stillBlocked[access]), { id: "organization-blocked" });
      }
    } finally {
      setChecking(false);
    }
  };

  useEffect(() => {
    const interval = setInterval(() => check(false), POLL_INTERVAL_MS);
    return () => clearInterval(interval);
  }, []);

  return (
    <div className="flex min-h-screen items-center justify-center bg-slate-50 px-6 py-12">
      <div className="w-full max-w-lg">
        <div className="rounded-xl border border-slate-200 bg-card p-8 shadow-sm">
          <div className="flex size-11 items-center justify-center rounded-lg bg-brand-50">
            <Icon className="size-5 text-brand-800" />
          </div>

          <p className="mt-6 font-mono text-xs uppercase tracking-widest text-slate-400">
            {t(eyebrowKey)}
          </p>
          <h1 className="mt-2 text-2xl font-semibold tracking-tight text-slate-900">
            {t(titleKey)}
          </h1>
          <p className="mt-3 text-sm leading-relaxed text-slate-500">{t(bodyKey)}</p>

          {access === "pending" && user && (
            <div className="mt-6">
              <p className="mb-2 text-sm text-slate-500">
                {t("organizationGate.sendTheseToYourAdmin")}
              </p>
              <div className="divide-y divide-slate-100 rounded-lg border border-slate-200 bg-slate-50/60">
                <CopyableField label={t("common.username")} value={user.userName} />
                <CopyableField label={t("common.email")} value={user.email} />
              </div>
            </div>
          )}

          {access !== "pending" && user?.organization && (
            <div className="mt-6 rounded-lg border border-slate-200 bg-slate-50/60 px-4 py-3">
              <p className="text-xs font-medium uppercase tracking-wide text-slate-400">
                {t("common.organization")}
              </p>
              <p className="font-mono text-sm text-slate-700">
                {user.organization.friendlyName}
              </p>
            </div>
          )}

          <div className="mt-7 flex flex-wrap items-center gap-3">
            <Button onClick={() => check(true)} disabled={checking}>
              <RefreshCw className={checking ? "animate-spin" : ""} />
              {t("organizationGate.checkAgain")}
            </Button>
            <Button variant="outline" asChild>
              <Link href="/auth/logout">
                <LogOut />
                {t("organizationGate.signOut")}
              </Link>
            </Button>
          </div>

          <p className="mt-6 text-xs text-slate-400">
            {t("organizationGate.checkingAutomaticallyEvery15Seconds")}
          </p>
        </div>
      </div>
    </div>
  );
}
