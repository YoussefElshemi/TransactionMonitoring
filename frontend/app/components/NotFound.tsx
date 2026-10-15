import Link from "next/link";
import { Button } from "@/components/ui/button";

export function NotFound({
  title,
  description,
  backHref,
  backLabel,
}: {
  title: string;
  description: string;
  backHref?: string;
  backLabel?: string;
}) {
  return (
    <div className="flex min-h-[60vh] flex-col items-center justify-center px-6 text-center">
      <p className="font-mono text-xs uppercase tracking-widest text-slate-400">
        404
      </p>
      <h1 className="mt-2 font-mono text-xl font-medium tracking-tight text-slate-900">
        {title}
      </h1>
      <p className="mt-2 max-w-sm text-sm leading-relaxed text-slate-500">
        {description}
      </p>
      {backHref && backLabel && (
        <Button variant="outline" className="mt-6" asChild>
          <Link href={backHref}>{backLabel}</Link>
        </Button>
      )}
    </div>
  );
}
