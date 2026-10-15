import type { Metadata } from "next";
import Link from "next/link";

import { Mark } from "@/components/Mark";
import { ThemeToggle } from "@/components/ThemeToggle";

export const metadata: Metadata = {
  title: "Service status — Transaction Monitoring",
  description:
    "Live component health, open incidents and scheduled maintenance for the Transaction Monitoring platform.",
};

export default function StatusLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <div className="flex min-h-screen flex-col bg-slate-50">
      <header className="border-b border-slate-200 bg-console">
        <div className="mx-auto flex w-full max-w-4xl items-center justify-between gap-4 px-6 py-4">
          <Link
            href="/status"
            className="flex items-center gap-2.5 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-600"
          >
            <Mark className="size-4 flex-shrink-0 text-brand-600" />
            <span className="font-mono text-sm font-medium tracking-tight text-white">
              Transaction Monitor
            </span>
            <span className="font-mono text-xs uppercase tracking-widest text-console-500">
              Status
            </span>
          </Link>
          <ThemeToggle />
        </div>
      </header>

      <main className="mx-auto w-full max-w-4xl flex-1 px-6 py-10">
        {children}
      </main>

      <footer className="border-t border-slate-200 px-6 py-6">
        <div className="mx-auto flex w-full max-w-4xl flex-wrap items-center justify-between gap-3">
          <p className="font-mono text-xs uppercase tracking-widest text-slate-400">
            Transaction Monitoring
          </p>
          <Link
            href="/"
            className="text-xs text-slate-500 underline underline-offset-4 hover:text-slate-700"
          >
            Sign in to the console
          </Link>
        </div>
      </footer>
    </div>
  );
}
