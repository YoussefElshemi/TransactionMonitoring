"use client";

import { useState } from "react";
import { Check, Copy, TriangleAlert } from "lucide-react";
import { toast } from "sonner";

import { Button } from "@/components/ui/button";
import { useTranslation } from "@/context/LocalizationContext";
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle
} from "@/components/ui/dialog";

export function SecretRevealDialog({
  secret,
  onClose,
  title = "Copy your signing secret",
  description
}: {
  secret: string | null;
  onClose: () => void;
  title?: string;
  description: string;
}) {
  const t = useTranslation();
  const [copied, setCopied] = useState(false);

  const handleCopy = async () => {
    if (!secret) return;

    try {
      await navigator.clipboard.writeText(secret);
      setCopied(true);
      setTimeout(() => setCopied(false), 1500);
    } catch {
      toast.error(t("common.copyingIsnTAvailableBrowser"));
    }
  };

  const handleClose = () => {
    setCopied(false);
    onClose();
  };

  return (
    <Dialog
      open={secret !== null}
      onOpenChange={(open) => !open && handleClose()}
    >
      <DialogContent className="sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>{title}</DialogTitle>
        </DialogHeader>

        <p className="text-sm text-slate-600">{description}</p>

        <div className="flex items-start gap-2.5 rounded-lg border border-attention-200 bg-attention-50 px-3 py-2.5">
          <TriangleAlert className="mt-0.5 size-4 flex-shrink-0 text-attention-700" />
          <p className="text-sm text-attention-700">
            This is the only time the secret is shown. Once you close this
            dialog it cannot be retrieved — you would have to rotate it and
            update your endpoint again.
          </p>
        </div>

        <div className="overflow-hidden rounded-xl border border-console-border bg-console">
          <div className="flex items-center justify-between border-b border-white/10 px-4 py-2">
            <span className="font-mono text-[11px] uppercase tracking-widest text-console-500">
              signing secret
            </span>
            <Button
              variant="ghost"
              size="sm"
              className="h-auto gap-1.5 px-1.5 py-0.5 font-mono text-[11px] text-console-400 hover:bg-white/5 hover:text-console-200"
              onClick={handleCopy}
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
          <p className="break-all px-4 py-3 font-mono text-xs text-console-200">
            {secret}
          </p>
        </div>

        <DialogFooter>
          <Button onClick={handleClose}>I have stored it</Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
