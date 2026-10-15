import Link from "next/link";
import { ArrowLeft, ArrowRight } from "lucide-react";

import { CopyButton } from "@/components/docs/CopyButton";
import { docsNeighbours } from "@/lib/docs/site";
import type { HttpMethod } from "@/lib/docs/reference";
import { cn } from "@/lib/utils";

export function DocPage({
  href,
  title,
  lead,
  children,
}: {
  href: string;
  title: string;
  lead: React.ReactNode;
  children: React.ReactNode;
}) {
  const { previous, next } = docsNeighbours(href);

  return (
    <article className="min-w-0">
      <h1 className="font-mono text-2xl font-medium tracking-tight text-slate-900">{title}</h1>
      <p className="mt-3 max-w-3xl text-base leading-relaxed text-slate-600">{lead}</p>
      <div className="mt-8 space-y-5">{children}</div>

      <div className="mt-14 flex flex-wrap items-center justify-between gap-4 border-t border-slate-200 pt-6">
        {previous ? (
          <Link href={previous.href} className={cn(LINK, "inline-flex items-center gap-1.5 text-sm")}>
            <ArrowLeft className="size-3.5" />
            {previous.label}
          </Link>
        ) : (
          <span />
        )}
        {next && (
          <Link href={next.href} className={cn(LINK, "inline-flex items-center gap-1.5 text-sm")}>
            {next.label}
            <ArrowRight className="size-3.5" />
          </Link>
        )}
      </div>
    </article>
  );
}

const LINK =
  "text-brand-800 underline decoration-brand-300 underline-offset-4 hover:decoration-brand-600 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-600";

export function A({ href, children }: { href: string; children: React.ReactNode }) {
  return (
    <Link href={href} className={LINK}>
      {children}
    </Link>
  );
}

export function H2({ id, children }: { id: string; children: React.ReactNode }) {
  return (
    <h2
      id={id}
      className="scroll-mt-24 border-t border-slate-200 pt-8 font-mono text-lg font-medium tracking-tight text-slate-900 first:border-0 first:pt-0"
    >
      {children}
    </h2>
  );
}

export function H3({ id, children }: { id?: string; children: React.ReactNode }) {
  return (
    <h3 id={id} className="scroll-mt-24 pt-2 font-mono text-sm font-medium tracking-tight text-slate-900">
      {children}
    </h3>
  );
}

export function P({ children }: { children: React.ReactNode }) {
  return <p className="max-w-3xl text-sm leading-relaxed text-slate-600">{children}</p>;
}

export function List({ ordered, children }: { ordered?: boolean; children: React.ReactNode }) {
  const Tag = ordered ? "ol" : "ul";

  return (
    <Tag
      className={cn(
        "max-w-3xl space-y-2 pl-5 text-sm leading-relaxed text-slate-600 marker:text-slate-400",
        ordered ? "list-decimal" : "list-disc"
      )}
    >
      {children}
    </Tag>
  );
}

export function Code({ children }: { children: React.ReactNode }) {
  return (
    <code className="rounded bg-slate-100 px-1 py-0.5 font-mono text-[0.85em] text-slate-800">
      {children}
    </code>
  );
}

export function CodeBlock({ label, code }: { label: string; code: string }) {
  return (
    <div className="overflow-hidden rounded-lg border border-console-border bg-console">
      <div className="flex items-center justify-between border-b border-console-border px-4 py-1.5">
        <span className="font-mono text-[11px] uppercase tracking-widest text-console-500">{label}</span>
        <CopyButton value={code} />
      </div>
      <pre className="overflow-x-auto p-4 font-mono text-xs leading-relaxed text-console-200">
        <code>{code}</code>
      </pre>
    </div>
  );
}

export function DocTable({ head, rows }: { head: string[]; rows: React.ReactNode[][] }) {
  return (
    <div className="overflow-x-auto rounded-lg border border-slate-200 bg-card">
      <table className="w-full text-left text-sm">
        <thead>
          <tr className="border-b border-slate-200 bg-slate-50">
            {head.map((heading) => (
              <th
                key={heading}
                scope="col"
                className="whitespace-nowrap px-4 py-2 font-mono text-[11px] font-medium uppercase tracking-widest text-slate-500"
              >
                {heading}
              </th>
            ))}
          </tr>
        </thead>
        <tbody className="divide-y divide-slate-100">
          {rows.map((row, rowIndex) => (
            <tr key={rowIndex}>
              {row.map((cell, cellIndex) => (
                <td key={cellIndex} className="px-4 py-2.5 align-top leading-relaxed text-slate-600">
                  {cell}
                </td>
              ))}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

export function Note({ children }: { children: React.ReactNode }) {
  return (
    <div className="max-w-3xl rounded-md border border-attention-200 bg-attention-50 px-4 py-3 text-sm leading-relaxed text-attention-700">
      {children}
    </div>
  );
}

const METHOD_STYLES: Record<HttpMethod, string> = {
  GET: "border-settled-200 bg-settled-50 text-settled-700",
  POST: "border-inflight-200 bg-inflight-50 text-inflight-700",
  PUT: "border-attention-200 bg-attention-50 text-attention-700",
  PATCH: "border-elevated-200 bg-elevated-50 text-elevated-700",
  DELETE: "border-transparent bg-critical-solid text-critical-solid-foreground",
};

export function MethodBadge({ method }: { method: HttpMethod }) {
  return (
    <span
      className={cn(
        "inline-flex w-14 flex-shrink-0 justify-center rounded border px-1.5 py-0.5 font-mono text-[11px] font-medium",
        METHOD_STYLES[method]
      )}
    >
      {method}
    </span>
  );
}

export function Endpoint({ method, path }: { method: HttpMethod; path: string }) {
  return (
    <span className="inline-flex items-center gap-2 align-middle">
      <MethodBadge method={method} />
      <span className="break-all font-mono text-xs text-slate-800">{path}</span>
    </span>
  );
}
