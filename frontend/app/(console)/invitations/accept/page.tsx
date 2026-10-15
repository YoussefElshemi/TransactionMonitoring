"use client";

import { useCallback, useEffect, useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import Link from "next/link";
import { toast } from "sonner";

import { AcceptedInvitation, InvitationPreview } from "@/types";
import { unwrap } from "@/lib/api";
import { apiFetch } from "@/lib/http";
import {
  TOKEN_PARAM,
  acceptInvitationPath,
  previewInvitationPath,
  readInvitationAlreadyMember,
  roleName,
} from "@/lib/invitations";
import { loginHref } from "@/lib/authRedirect";
import { useAuth } from "@/context/AuthContext";
import { useTranslation } from "@/context/LocalizationContext";
import { useFormat } from "@/lib/useFormat";
import { Mark } from "@/components/Mark";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";

type Stage =
  | "loading"
  | "offer"
  | "invalid"
  | "alreadyHere"
  | "elsewhere"
  | "spokenFor";

export default function AcceptInvitation() {
  const t = useTranslation();
  const format = useFormat();
  const router = useRouter();
  const searchParams = useSearchParams();
  const { logIn } = useAuth();

  const token = searchParams.get(TOKEN_PARAM) ?? "";

  const [stage, setStage] = useState<Stage>("loading");
  const [preview, setPreview] = useState<InvitationPreview | null>(null);
  const [userName, setUserName] = useState("");
  const [password, setPassword] = useState("");
  const [submitting, setSubmitting] = useState(false);

  const loadPreview = useCallback(async () => {
    if (!token) {
      setStage("invalid");
      return;
    }

    try {
      const response = await apiFetch(previewInvitationPath(token), {
        method: "GET",
        headers: { "Content-Type": "application/json" },
        credentials: "include",
      });

      if (!response.ok) {
        setStage("invalid");
        return;
      }

      const offer = await unwrap<InvitationPreview>(response);
      setPreview(offer);
      setUserName(offer.email);
      setStage("offer");
    } catch {
      setStage("invalid");
    }
  }, [token]);

  useEffect(() => {
    void loadPreview();
  }, [loadPreview]);

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    setSubmitting(true);

    try {
      const response = await apiFetch(acceptInvitationPath(), {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        credentials: "include",
        body: JSON.stringify({ token, userName, password }),
      });

      if (response.ok) {
        const accepted = await unwrap<AcceptedInvitation>(response);

        if (accepted.isCreated) {
          await logIn(userName, password);
          router.replace("/");
          return;
        }

        toast.success(t("accept.joined", preview?.organizationName ?? ""), {
          description: t("accept.joinedExistingHelp"),
        });
        router.replace(loginHref("/"));
        return;
      }

      if (response.status === 409) {
        let member: { sameOrganization: boolean | null } | null = null;

        try {
          member = readInvitationAlreadyMember(await response.json());
        } catch {
        }

        setStage(
          member?.sameOrganization === true
            ? "alreadyHere"
            : member?.sameOrganization === false
              ? "elsewhere"
              : "spokenFor"
        );
        return;
      }

      setStage("invalid");
    } catch {
      toast.error(t("accept.error"));
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="flex min-h-screen items-center justify-center bg-slate-50 px-6 py-10">
      <div className="w-full max-w-sm">
        <Link
          href="/"
          className="mb-8 flex items-center justify-center gap-2.5 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-800"
        >
          <Mark className="size-5 text-brand-800" />
          <span className="font-mono text-sm font-medium tracking-tight text-slate-900">
            {t("auth.transactionMonitor")}
          </span>
        </Link>

        <div className="rounded-xl border border-slate-200 bg-card p-8">
          {stage === "loading" && (
            <div className="space-y-3">
              <Skeleton className="h-4 w-24" />
              <Skeleton className="h-6 w-52" />
              <Skeleton className="h-24 w-full" />
            </div>
          )}

          {stage === "invalid" && (
            <>
              <p className="font-mono text-xs uppercase tracking-widest text-slate-400">
                {t("accept.eyebrow")}
              </p>
              <h1 className="mb-3 mt-1 font-mono text-xl font-medium tracking-tight text-slate-900">
                {t("accept.invalid.title")}
              </h1>
              <p className="text-sm leading-relaxed text-slate-500">
                {t("accept.invalid.description")}
              </p>
            </>
          )}

          {stage === "alreadyHere" && (
            <>
              <p className="font-mono text-xs uppercase tracking-widest text-slate-400">
                {t("accept.eyebrow")}
              </p>
              <h1 className="mb-3 mt-1 font-mono text-xl font-medium tracking-tight text-slate-900">
                {t("accept.alreadyHere.title")}
              </h1>
              <p className="text-sm leading-relaxed text-slate-500">
                {t("accept.alreadyHere.description")}
              </p>
              <p className="mt-4 text-center text-sm text-slate-500">
                <Link
                  href={loginHref("/")}
                  className="font-medium text-brand-800 hover:text-brand-900"
                >
                  {t("common.signIn")}
                </Link>
              </p>
            </>
          )}

          {stage === "elsewhere" && (
            <>
              <p className="font-mono text-xs uppercase tracking-widest text-slate-400">
                {t("accept.eyebrow")}
              </p>
              <h1 className="mb-3 mt-1 font-mono text-xl font-medium tracking-tight text-slate-900">
                {t("accept.elsewhere.title")}
              </h1>
              <p className="text-sm leading-relaxed text-slate-500">
                {t("accept.elsewhere.description")}
              </p>
              <p className="mt-4 text-center text-sm text-slate-500">
                <Link
                  href={loginHref("/")}
                  className="font-medium text-brand-800 hover:text-brand-900"
                >
                  {t("common.signIn")}
                </Link>
              </p>
            </>
          )}

          {stage === "spokenFor" && (
            <>
              <p className="font-mono text-xs uppercase tracking-widest text-slate-400">
                {t("accept.eyebrow")}
              </p>
              <h1 className="mb-3 mt-1 font-mono text-xl font-medium tracking-tight text-slate-900">
                {t("accept.spokenFor.title")}
              </h1>
              <p className="text-sm leading-relaxed text-slate-500">
                {t("accept.spokenFor.description")}
              </p>
              <p className="mt-4 text-center text-sm text-slate-500">
                <Link
                  href={loginHref("/")}
                  className="font-medium text-brand-800 hover:text-brand-900"
                >
                  {t("common.signIn")}
                </Link>
              </p>
            </>
          )}

          {stage === "offer" && preview && (
            <>
              <p className="font-mono text-xs uppercase tracking-widest text-slate-400">
                {t("accept.eyebrow")}
              </p>
              <h1 className="mt-1 font-mono text-xl font-medium tracking-tight text-slate-900">
                {t("accept.title", preview.organizationName)}
              </h1>

              <dl className="mb-6 mt-4 space-y-2 rounded-lg border border-slate-200 bg-slate-50 px-4 py-3">
                <Offered label={t("common.email")} value={preview.email} />
                <Offered
                  label={t("invitations.invite.role")}
                  value={roleName(t, preview.role)}
                />
                <Offered
                  label={t("invitations.expires")}
                  value={format.relative(preview.expiresAt)}
                />
              </dl>

              <form onSubmit={handleSubmit} className="space-y-4">
                <div className="space-y-1.5">
                  <label
                    htmlFor="accept-username"
                    className="font-mono text-xs uppercase tracking-widest text-slate-400"
                  >
                    {t("common.username")}
                  </label>
                  <Input
                    id="accept-username"
                    type="text"
                    value={userName}
                    onChange={(event) => setUserName(event.target.value)}
                    required
                    autoComplete="username"
                    disabled={submitting}
                  />
                </div>
                <div className="space-y-1.5">
                  <label
                    htmlFor="accept-password"
                    className="font-mono text-xs uppercase tracking-widest text-slate-400"
                  >
                    {t("auth.password")}
                  </label>
                  <Input
                    id="accept-password"
                    type="password"
                    value={password}
                    onChange={(event) => setPassword(event.target.value)}
                    required
                    autoComplete="new-password"
                    disabled={submitting}
                  />
                  <p className="text-xs text-slate-500">
                    {t("accept.passwordHelp")}
                  </p>
                </div>
                <Button type="submit" className="w-full" disabled={submitting}>
                  {submitting ? t("accept.joining") : t("accept.action")}
                </Button>
              </form>
            </>
          )}
        </div>
      </div>
    </div>
  );
}

function Offered({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex items-baseline justify-between gap-3">
      <dt className="font-mono text-[11px] uppercase tracking-widest text-slate-400">
        {label}
      </dt>
      <dd className="min-w-0 truncate text-sm text-slate-900">{value}</dd>
    </div>
  );
}
