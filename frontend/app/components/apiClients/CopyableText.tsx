"use client";

import { useState } from "react";
import { Check, Copy } from "lucide-react";
import { toast } from "sonner";

import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";
import { useTranslation } from "@/context/LocalizationContext";

export function CopyableText({
  value,
  display,
  className
}: {
  value: string;
  display?: string;
  className?: string;
}) {
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
    <span className={cn("inline-flex items-center gap-1.5", className)}>
      <span
        className="break-all font-mono text-xs text-slate-600"
        title={display ? value : undefined}
      >
        {display ?? value}
      </span>
      <Button
        variant="ghost"
        size="icon"
        className="h-6 w-6 flex-shrink-0 text-slate-400 hover:text-slate-700"
        onClick={handleCopy}
        title={copied ? "Copied" : "Copy"}
        aria-label={copied ? "Copied" : "Copy"}
      >
        {copied ? <Check className="size-3.5" /> : <Copy className="size-3.5" />}
        <span className="sr-only">{copied ? "Copied" : "Copy"}</span>
      </Button>
    </span>
  );
}
