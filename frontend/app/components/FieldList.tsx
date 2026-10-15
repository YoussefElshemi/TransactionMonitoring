import { cn } from "@/lib/utils";

export function FieldList({
  children,
  className,
}: {
  children: React.ReactNode;
  className?: string;
}) {
  return (
    <dl className={cn("divide-y divide-slate-100", className)}>{children}</dl>
  );
}

export function Field({
  label,
  children,
  mono = false,
}: {
  label: string;
  children: React.ReactNode;
  mono?: boolean;
}) {
  return (
    <div className="flex flex-wrap items-baseline justify-between gap-x-6 gap-y-1 px-6 py-3.5">
      <dt className="font-mono text-xs uppercase tracking-widest text-slate-400">
        {label}
      </dt>
      <dd
        className={cn(
          "min-w-0 text-right text-sm text-slate-700",
          mono && "font-mono"
        )}
      >
        {children}
      </dd>
    </div>
  );
}
