import type { Metadata } from "next";
import Link from "next/link";

import { DocsNav } from "@/components/docs/DocsNav";
import { Mark } from "@/components/Mark";
import { ThemeToggle } from "@/components/ThemeToggle";
import { DOCS_API_VERSION } from "@/lib/docs/site";

export const metadata: Metadata = {
  title: {
    default: "API documentation — Transaction Monitoring",
    template: "%s — Transaction Monitoring API",
  },
  description:
    "How to authenticate, screen transactions, receive webhooks and handle errors with the Transaction Monitoring API.",
};

const HEADER_LINK =
  "font-mono text-xs uppercase tracking-widest text-console-400 transition-colors hover:text-white focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-600";

export default function DocsLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <div className="flex min-h-screen flex-col bg-slate-50">
      <header className="sticky top-0 z-20 border-b border-slate-200 bg-console">
        <div className="mx-auto flex w-full max-w-6xl items-center justify-between gap-4 px-6 py-4">
          <Link
            href="/docs"
            className="flex min-w-0 items-center gap-2.5 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-600"
          >
            <Mark className="size-4 flex-shrink-0 text-brand-600" />
            <span className="truncate font-mono text-sm font-medium tracking-tight text-white">
              Transaction Monitor
            </span>
            <span className="hidden font-mono text-xs uppercase tracking-widest text-console-500 sm:inline">
              API
            </span>
          </Link>
          <div className="flex flex-shrink-0 items-center gap-3 sm:gap-5">
            <span className="hidden font-mono text-xs text-console-500 sm:inline">
              {DOCS_API_VERSION}
            </span>
            <Link href="/status" className={HEADER_LINK}>
              Status
            </Link>
            <Link href="/" className={HEADER_LINK}>
              Console
            </Link>
            <ThemeToggle />
          </div>
        </div>
      </header>

      <div className="mx-auto flex w-full max-w-6xl flex-1 flex-col gap-8 px-6 py-10 lg:flex-row lg:gap-12">
        <aside className="lg:w-56 lg:flex-shrink-0">
          <div className="lg:sticky lg:top-24">
            <DocsNav />
          </div>
        </aside>
        <main className="min-w-0 flex-1">{children}</main>
      </div>

      <footer className="border-t border-slate-200 px-6 py-6">
        <div className="mx-auto flex w-full max-w-6xl flex-wrap items-center justify-between gap-3">
          <p className="font-mono text-xs uppercase tracking-widest text-slate-400">
            Transaction Monitoring
          </p>
          <p className="text-xs text-slate-500">
            Quote the <span className="font-mono">X-Correlation-ID</span> of a response when reporting a problem.
          </p>
        </div>
      </footer>
    </div>
  );
}
