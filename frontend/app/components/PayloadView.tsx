"use client";

import { useState } from "react";
import CodeMirror from "@uiw/react-codemirror";
import { json } from "@codemirror/lang-json";
import { Check, Copy, Eye, EyeOff } from "lucide-react";
import { toast } from "sonner";

import { cn } from "@/lib/utils";
import { PayloadFields } from "@/components/PayloadFields";
import { useTranslation } from "@/context/LocalizationContext";

export function PayloadView({
  payload,
  label = "payload",
  masked = false,
  defaultView = "fields",
  onReveal,
  revealing = false,
}: {
  payload: unknown;
  label?: string;
  defaultView?: "fields" | "json";
  masked?: boolean;
  onReveal?: () => void;
  revealing?: boolean;
}) {
  const t = useTranslation();
  const [copied, setCopied] = useState(false);
  const [view, setView] = useState<"fields" | "json">(defaultView);
  const value = JSON.stringify(payload, null, 2);

  const handleCopy = async () => {
    try {
      await navigator.clipboard.writeText(value);
      setCopied(true);
      setTimeout(() => setCopied(false), 1500);

      if (masked) {
        toast.info(t("payloadFields.copiedValuesWithheld"), {
          id: "copied-masked-payload",
          description:
            t("payloadFields.structurePaymentSEveryValue"),
        });
      }
    } catch {
      toast.error(t("common.copyingIsnTAvailableBrowser"));
    }
  };

  return (
    <div className="overflow-hidden rounded-xl border border-console-border bg-console">
      <div className="flex items-center justify-between gap-3 border-b border-white/10 px-4 py-2">
        <div className="flex min-w-0 items-center gap-2">
          <span className="font-mono text-[11px] uppercase tracking-widest text-console-500">
            {label}
          </span>
          {masked && (
            <span
              className="flex min-w-0 items-center gap-1.5 font-mono text-[11px] uppercase tracking-widest text-console-400"
              title={t("payloadView.everyValueIsWithheldKeys")}
            >
              <EyeOff aria-hidden className="size-3 flex-shrink-0" />
              <span className="truncate">values withheld</span>
            </span>
          )}
        </div>
        <div className="flex flex-shrink-0 items-center gap-1">
          <div
            role="group"
            aria-label={t("payloadView.payloadView")}
            className="mr-1 flex items-center rounded-md border border-white/10 p-0.5"
          >
            {(["fields", "json"] as const).map((option) => (
              <button
                key={option}
                type="button"
                aria-pressed={view === option}
                onClick={() => setView(option)}
                className={cn(
                  "rounded px-1.5 py-0.5 font-mono text-[11px] transition-colors focus:outline-none focus:ring-2 focus:ring-brand-600",
                  view === option
                    ? "bg-white/10 text-console-200"
                    : "text-console-500 hover:text-console-300"
                )}
              >
                {option === "fields" ? "fields" : "json"}
              </button>
            ))}
          </div>
          {onReveal && (
            <button
              type="button"
              onClick={onReveal}
              disabled={revealing}
              className="flex items-center gap-1.5 rounded-md px-1.5 py-0.5 font-mono text-[11px] text-console-400 transition-colors hover:bg-white/5 hover:text-console-200 focus:outline-none focus:ring-2 focus:ring-brand-600 disabled:opacity-50"
            >
              <Eye aria-hidden className="size-3" />
              {revealing ? "revealing…" : "reveal"}
            </button>
          )}
          <button
            type="button"
            onClick={handleCopy}
            aria-label={masked ? t("payloadFields.copyMaskedPayload") : t("organization.copyPayload")}
            className="rounded-md p-1 text-console-500 transition-colors hover:text-console-300 focus:outline-none focus:ring-2 focus:ring-brand-600"
          >
            {copied ? (
              <Check className="size-3.5 text-settled-500" />
            ) : (
              <Copy className="size-3.5" />
            )}
          </button>
        </div>
      </div>

      {masked && (
        <p className="border-b border-white/10 px-4 py-2 text-xs leading-relaxed text-console-400">
          Values are withheld by default. The keys, the nesting and the list
          lengths are the payment&apos;s own — only the values are not.
          {onReveal
            ? " Revealing shows it in the clear and is recorded against this payment."
            : " There is no way to see this one in the clear from here."}{" "}
          A card shows its last four: the full number was never stored, so
          nothing recovers it.
        </p>
      )}

      {view === "fields" ? (
        <PayloadFields payload={payload} />
      ) : (
        <CodeMirror
          readOnly
          editable={false}
          value={value}
          extensions={[json()]}
          theme="dark"
          basicSetup={{
            foldGutter: false,
            highlightActiveLine: false,
            highlightActiveLineGutter: false,
          }}
          className="[&_.cm-activeLine]:!bg-transparent [&_.cm-editor]:!bg-transparent [&_.cm-gutters]:!border-none [&_.cm-gutters]:!bg-transparent [&_.cm-gutterElement]:!text-console-600"
        />
      )}
    </div>
  );
}
