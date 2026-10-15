"use client";

import { useState } from "react";
import { useSearchParams } from "next/navigation";
import { useAuth } from "@/context/AuthContext";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import { Mark } from "@/components/Mark";
import { loginHref, safeRedirectUri } from "@/lib/authRedirect";
import { useSessionRedirect } from "@/lib/useSessionRedirect";
import Link from "next/link";
import { useTranslation } from "@/context/LocalizationContext";

const Register = () => {
  const t = useTranslation();
  const { register } = useAuth();
  const [username, setUsername] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const searchParams = useSearchParams();
  const redirectUri = safeRedirectUri(searchParams?.get("redirect_uri"));

  const showForm = useSessionRedirect(redirectUri);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    await register(username, email, password);
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
            create account
          </p>
          <h1 className="mt-1 font-mono text-xl font-medium tracking-tight text-slate-900">
            {t("auth.getStarted")}
          </h1>
          <p className="mb-6 mt-2 text-sm leading-relaxed text-slate-500">
            After you sign up, an admin adds you to your organization. You&apos;ll
            see a waiting screen until they do.
          </p>
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
                htmlFor="email"
                className="font-mono text-xs uppercase tracking-widest text-slate-400"
              >
                {t("common.email")}
              </label>
              <Input
                id="email"
                type="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                required
                autoComplete="email"
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
                autoComplete="new-password"
              />
            </div>
            <Button type="submit" className="w-full">
              {t("auth.createAccount")}
            </Button>
          </form>
          <p className="mt-4 text-center text-sm text-slate-500">
            Already have an account?{" "}
            <Link
              href={loginHref(redirectUri)}
              className="font-medium text-brand-800 hover:text-brand-900"
            >
              {t("common.signIn")}
            </Link>
          </p>
        </div>
      </div>
    </div>
  );
};

export default Register;
