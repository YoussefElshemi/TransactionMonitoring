"use client";

import { useState } from "react";
import { Check, Copy, TriangleAlert } from "lucide-react";
import { toast } from "sonner";

import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle
} from "@/components/ui/dialog";
import { useTranslation } from "@/context/LocalizationContext";

export function ClientCredentialsDialog({
  clientId,
  clientSecret,
  title,
  description,
  onAcknowledge
}: {
  clientId: string;
  clientSecret: string | null;
  title: string;
  description: string;
  onAcknowledge: () => void;
}) {
  const t = useTranslation();
  const [copied, setCopied] = useState<string | null>(null);

  const copy = async (label: string, value: string) => {
    try {
      await navigator.clipboard.writeText(value);
      setCopied(label);
      setTimeout(() => setCopied((current) => (current === label ? null : current)), 1500);
    } catch {
      toast.error(t("common.copyingIsnTAvailableBrowser"));
    }
  };

  const handleAcknowledge = () => {
    setCopied(null);
    onAcknowledge();
  };

  return (
    <Dialog open={clientSecret !== null}>
      <DialogContent
        className="sm:max-w-lg [&>button]:hidden"
        onEscapeKeyDown={(event) => event.preventDefault()}
        onPointerDownOutside={(event) => event.preventDefault()}
        onInteractOutside={(event) => event.preventDefault()}
      >
        <DialogHeader>
          <DialogTitle>{title}</DialogTitle>
        </DialogHeader>

        <p className="text-sm text-slate-600">{description}</p>

        <div className="flex items-start gap-2.5 rounded-lg border border-attention-200 bg-attention-50 px-3 py-2.5">
          <TriangleAlert className="mt-0.5 size-4 flex-shrink-0 text-attention-700" />
          <p className="text-sm text-attention-700">
            This is the only time the secret is shown. Once you close this
            dialog it cannot be retrieved — you would have to add another secret
            and update the caller again.
          </p>
        </div>

        <div className="overflow-hidden rounded-xl border border-console-border bg-console">
          <CredentialRow
            label={t("apiClients.clientId2")}
            value={clientId}
            copied={copied === "client id"}
            onCopy={() => copy("client id", clientId)}
          />
          <CredentialRow
            label={t("apiClients.clientSecret")}
            value={clientSecret ?? ""}
            copied={copied === "client secret"}
            onCopy={() => copy("client secret", clientSecret ?? "")}
          />
        </div>

        <div className="flex flex-wrap items-center justify-between gap-2">
          <p className="text-xs text-console-500">
            Exchange the pair at{" "}
            <span className="font-mono text-slate-600">{t("apiClients.postOauthToken")}</span>{" "}
            for an access token, then call the API with that token.
          </p>
          <Button
            variant="outline"
            size="sm"
            className="gap-1.5"
            onClick={() =>
              copy(
                "both",
                `CLIENT_ID=${clientId}\nCLIENT_SECRET=${clientSecret ?? ""}`
              )
            }
          >
            {copied === "both" ? (
              <>
                <Check className="size-4" />
                {t("apiClients.copied")}
              </>
            ) : (
              <>
                <Copy className="size-4" />
                {t("apiClients.copyBoth")}
              </>
            )}
          </Button>
        </div>

        <DialogFooter>
          <Button onClick={handleAcknowledge}>{t("apiClients.iHaveStoredTheSecret")}</Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

function CredentialRow({
  label,
  value,
  copied,
  onCopy
}: {
  label: string;
  value: string;
  copied: boolean;
  onCopy: () => void;
}) {
  return (
    <div className="border-b border-white/10 last:border-b-0">
      <div className="flex items-center justify-between px-4 py-2">
        <span className="font-mono text-[11px] uppercase tracking-widest text-console-500">
          {label}
        </span>
        <Button
          variant="ghost"
          size="sm"
          className="h-auto gap-1.5 px-1.5 py-0.5 font-mono text-[11px] text-console-400 hover:bg-white/5 hover:text-console-200"
          onClick={onCopy}
        >
          {copied ? (
            <>
              <Check className="size-3" />
              copied
            </>
          ) : (
            <>
              <Copy className="size-3" />
              copy
            </>
          )}
        </Button>
      </div>
      <p className="break-all px-4 pb-3 font-mono text-xs text-console-200">
        {value}
      </p>
    </div>
  );
}
