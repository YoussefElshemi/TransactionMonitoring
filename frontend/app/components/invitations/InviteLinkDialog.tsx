"use client";

import { useState } from "react";
import { Check, Copy, Mail } from "lucide-react";
import { toast } from "sonner";

import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { useTranslation } from "@/context/LocalizationContext";

export function InviteLinkDialog({
  acceptUrl,
  email,
  reissued,
  onClose,
}: {
  acceptUrl: string | null;
  email: string;
  reissued: boolean;
  onClose: () => void;
}) {
  const t = useTranslation();
  const [copied, setCopied] = useState(false);

  const handleCopy = async () => {
    if (!acceptUrl) return;

    try {
      await navigator.clipboard.writeText(acceptUrl);
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
      open={acceptUrl !== null}
      onOpenChange={(open) => !open && handleClose()}
    >
      <DialogContent className="sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>
            {reissued
              ? t("invitations.link.titleResent")
              : t("invitations.link.titleSent")}
          </DialogTitle>
        </DialogHeader>

        <div className="flex items-start gap-2.5 rounded-lg border border-settled-200 bg-settled-50 px-3 py-2.5">
          <Mail className="mt-0.5 size-4 flex-shrink-0 text-settled-700" />
          <p className="text-sm text-settled-700">
            {t("invitations.link.emailed", email)}
          </p>
        </div>

        <p className="text-sm text-slate-600">
          {reissued
            ? t("invitations.link.descriptionResent")
            : t("invitations.link.description")}
        </p>

        <div className="overflow-hidden rounded-xl border border-console-border bg-console">
          <div className="flex items-center justify-between border-b border-white/10 px-4 py-2">
            <span className="font-mono text-[11px] uppercase tracking-widest text-console-500">
              {t("invitations.link.label")}
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
                  {t("common.copied")}
                </>
              ) : (
                <>
                  <Copy className="size-3" />
                  {t("common.copy")}
                </>
              )}
            </Button>
          </div>
          <p className="break-all px-4 py-3 font-mono text-xs text-console-200">
            {acceptUrl}
          </p>
        </div>

        <DialogFooter>
          <Button onClick={handleClose}>{t("common.done")}</Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
