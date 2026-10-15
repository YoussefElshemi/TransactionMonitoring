"use client";

import { useState } from "react";
import { useSearchParams } from "next/navigation";
import { useAuth } from "@/context/AuthContext";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import { Mark } from "@/components/Mark";
import { safeRedirectUri } from "@/lib/authRedirect";
import { useSessionRedirect } from "@/lib/useSessionRedirect";
import Link from "next/link";
import { useTranslation } from "@/context/LocalizationContext";

const Login = () => {
  const t = useTranslation();
  const { logIn } = useAuth();
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const searchParams = useSearchParams();
  const redirectUri = safeRedirectUri(searchParams?.get("redirect_uri"));

  const showForm = useSessionRedirect(redirectUri);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    await logIn(username, password);
  };

  if (!showForm) {
    return (
      <div className="flex h-screen items-center justify-center bg-slate-50">
        <div className="size-12 animate-spin rounded-full border-2 border-brand-600 border-t-transparent" />
      </div>
    );
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-slate-50 px-6">
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
          <p className="font-mono text-xs uppercase tracking-widest text-slate-400">
            sign in
          </p>
          <h1 className="mb-6 mt-1 font-mono text-xl font-medium tracking-tight text-slate-900">
            {t("auth.welcomeBack")}
          </h1>
          <form onSubmit={handleSubmit} className="space-y-4">
            <div className="space-y-1.5">
              <label
                htmlFor="username"
                className="font-mono text-xs uppercase tracking-widest text-slate-400"
              >
                {t("common.username")}
              </label>
              <Input
                id="username"
                type="text"
                value={username}
                onChange={(e) => setUsername(e.target.value)}
                required
                autoComplete="username"
              />
            </div>
            <div className="space-y-1.5">
              <label
                htmlFor="password"
                className="font-mono text-xs uppercase tracking-widest text-slate-400"
              >
                {t("auth.password")}
              </label>
              <Input
                id="password"
                type="password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                required
                autoComplete="current-password"
              />
            </div>
            <Button type="submit" className="w-full">
              {t("common.signIn")}
            </Button>
          </form>
          <p className="mt-4 text-center text-sm text-slate-500">
            No account?{" "}
            <Link
              href={`/auth/register?redirect_uri=${encodeURIComponent(redirectUri)}`}
              className="font-medium text-brand-800 hover:text-brand-900"
            >
              {t("auth.register")}
            </Link>
          </p>
        </div>
      </div>
    </div>
  );
};

export default Login;
