"use client";

import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";

import { HistoryPanel, HistoryPanelProps } from "./HistoryPanel";
import { useTranslation } from "@/context/LocalizationContext";

interface HistoryDialogProps extends Omit<HistoryPanelProps, "enabled"> {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  title: string;
}

export function HistoryDialog({
  open,
  onOpenChange,
  title,
  ...panel
}: HistoryDialogProps) {
  const t = useTranslation();
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-h-[85vh] overflow-y-auto sm:max-w-3xl">
        <DialogHeader>
          <DialogTitle>{t("common.history")}</DialogTitle>
          <DialogDescription>
            Every change recorded for {title}, newest first.
          </DialogDescription>
        </DialogHeader>
        <HistoryPanel {...panel} enabled={open} />
      </DialogContent>
    </Dialog>
  );
}
