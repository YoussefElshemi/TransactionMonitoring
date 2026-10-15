"use client";

import { useEffect, useMemo, useState } from "react";
import { Check, X } from "lucide-react";

import { CaseDisposition } from "@/types";
import { cn } from "@/lib/utils";
import {
  DISPOSITION_HINTS,
  PRIMARY_DISPOSITIONS,
  SECONDARY_DISPOSITIONS,
  dispositionLabelKey,
  requiresNote,
} from "@/lib/cases";
import { appliesTo, useReasonCodes } from "@/lib/reasonCodes";
import { Button } from "@/components/ui/button";
import { Textarea } from "@/components/ui/textarea";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { useTranslation } from "@/context/LocalizationContext";

export interface CaseClose {
  status: "Approved" | "Rejected";
  disposition: CaseDisposition | null;
  reasonCodeId: string | null;
  note: string | null;
}

export function CloseCaseDialog({
  open,
  onOpenChange,
  organizationId,
  submitting,
  onSubmit,
  concludes = true,
  subject = "case",
  contextLabel,
}: {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  organizationId: string | null | undefined;
  submitting: boolean;
  onSubmit: (close: CaseClose) => void;
  concludes?: boolean;
  subject?: "case" | "payment";
  contextLabel?: string;
}) {
  const t = useTranslation();
  const [status, setStatus] = useState<"Approved" | "Rejected" | null>(null);
  const [disposition, setDisposition] = useState<CaseDisposition | null>(null);
  const [reasonCodeId, setReasonCodeId] = useState<string | null>(null);
  const [note, setNote] = useState("");

  const { codes, loading: codesLoading, unavailable } = useReasonCodes(
    concludes ? organizationId : null
  );

  const applicableCodes = useMemo(
    () => codes.filter((code) => appliesTo(code, disposition)),
    [codes, disposition]
  );

  useEffect(() => {
    if (!open) return;
    setStatus(null);
    setDisposition(null);
    setReasonCodeId(null);
    setNote("");
  }, [open]);

  useEffect(() => {
    if (!reasonCodeId) return;
    if (!applicableCodes.some((code) => code.caseReasonCodeId === reasonCodeId)) {
      setReasonCodeId(null);
    }
  }, [applicableCodes, reasonCodeId]);

  const showReasonCodes = concludes && !codesLoading && applicableCodes.length > 0;
  const noteRequired =
    status !== null && requiresNote(status, concludes ? disposition : null);

  const blocked =
    status === null ||
    (concludes && disposition === null) ||
    (showReasonCodes && reasonCodeId === null) ||
    (noteRequired && note.trim().length === 0);

  const submit = () => {
    if (blocked || status === null) return;
    onSubmit({
      status,
      disposition: concludes ? disposition : null,
      reasonCodeId: showReasonCodes ? reasonCodeId : null,
      note: note.trim().length > 0 ? note.trim() : null,
    });
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>
            {t(
              subject === "case"
                ? "closeCase.titleCase"
                : "closeCase.titleTransaction"
            )}
          </DialogTitle>
          <DialogDescription>
            {t(
              subject === "case"
                ? "closeCase.descriptionCase"
                : concludes
                  ? "closeCase.descriptionLastTransaction"
                  : "closeCase.descriptionTransaction"
            )}
            {contextLabel && (
              <>
                {" "}
                <span className="font-mono text-xs">{contextLabel}</span>
              </>
            )}
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-5">
          <Choice
            label={t("cases.verdict")}
            hint={t("closeCase.verdictHint")}
            required
            options={[
              {
                value: "Approved",
                labelKey: "closeCase.release",
                hintKey: "closeCase.releaseHint",
                icon: <Check className="size-4" />,
                tone: "settled",
              },
              {
                value: "Rejected",
                labelKey: "closeCase.block",
                hintKey: "closeCase.blockHint",
                icon: <X className="size-4" />,
                tone: "critical",
              },
            ]}
            value={status}
            onChange={(value) => setStatus(value as "Approved" | "Rejected")}
          />

          {concludes && (
            <Choice
              label={t("cases.disposition")}
              hint={t("closeCase.dispositionHint")}
              required
              options={PRIMARY_DISPOSITIONS.map(dispositionOption)}
              secondary={{
                caption: t("closeCase.secondaryCaption"),
                options: SECONDARY_DISPOSITIONS.map(dispositionOption),
              }}
              value={disposition}
              onChange={(value) => setDisposition(value as CaseDisposition)}
            />
          )}

          {showReasonCodes && (
            <div className="space-y-1.5">
              <FieldLabel required>{t("cases.reasonCode2")}</FieldLabel>
              <Select
                value={reasonCodeId ?? ""}
                onValueChange={setReasonCodeId}
                disabled={disposition === null}
              >
                <SelectTrigger>
                  <SelectValue
                    placeholder={
                      disposition === null
                        ? t("cases.pickDispositionFirst")
                        : t("cases.chooseReasonCode")
                    }
                  />
                </SelectTrigger>
                <SelectContent>
                  {applicableCodes.map((code) => (
                    <SelectItem
                      key={code.caseReasonCodeId}
                      value={code.caseReasonCodeId}
                    >
                      <span className="font-mono">{code.code}</span>
                      <span className="text-slate-500"> — {code.description}</span>
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
              <p className="text-xs text-slate-500">
                Your organisation files closed cases under these. Only the codes
                that apply to the disposition above are offered.
              </p>
            </div>
          )}

          <div className="space-y-1.5">
            <FieldLabel required={noteRequired}>{t("cases.note")}</FieldLabel>
            <Textarea
              value={note}
              onChange={(event) => setNote(event.target.value)}
              rows={3}
              placeholder={
                noteRequired
                  ? t("cases.requiredSayWhatFoundWhy")
                  : "Optional."
              }
            />
            <p className="text-xs text-slate-500">
              {noteRequired
                ? disposition === "FalseNegative"
                  ? "A miss is what the rules will be retuned against, so say what was missed and how it surfaced."
                  : status === "Rejected"
                    ? "A blocked payment is one the customer will ask about, so this one is required."
                    : "A true positive is one the tuning figures will be read against, so this one is required."
                : "Written as a case note either way, so it shows up in the case's notes afterwards."}
            </p>
          </div>

          {concludes && unavailable && (
            <p className="rounded-md border border-slate-200 bg-slate-50 px-3 py-2 text-xs text-slate-600">
              Your organisation&apos;s reason codes could not be read with this
              account, so none are offered. If your organisation requires one,
              the close will be refused and a manager will have to file it.
            </p>
          )}
        </div>

        <DialogFooter>
          <Button
            variant="outline"
            onClick={() => onOpenChange(false)}
            disabled={submitting}
          >
            {t("common.cancel")}
          </Button>
          <Button onClick={submit} disabled={blocked || submitting}>
            {submitting
              ? "Saving..."
              : subject === "case"
                ? "Close case"
                : "Settle payment"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

const FieldLabel = ({
  children,
  required,
}: {
  children: React.ReactNode;
  required?: boolean;
}) => (
  <label className="flex items-center gap-1 font-mono text-xs uppercase tracking-widest text-slate-500">
    {children}
    {required && <span className="text-critical-700">*</span>}
  </label>
);

const TONES: Record<string, string> = {
  settled: "border-settled-500 bg-settled-50 text-settled-700",
  critical: "border-critical-500 bg-critical-50 text-critical-700",
  elevated: "border-elevated-500 bg-elevated-50 text-elevated-700",
  slate: "border-slate-400 bg-slate-50 text-slate-700",
};

interface ChoiceOption {
  value: string;
  labelKey: string;
  hintKey: string;
  icon?: React.ReactNode;
  tone: string;
}

const dispositionOption = (value: CaseDisposition): ChoiceOption => ({
  value,
  labelKey: dispositionLabelKey(value),
  hintKey: DISPOSITION_HINTS[value],
  tone:
    value === "TruePositive"
      ? "elevated"
      : value === "FalseNegative"
        ? "critical"
        : "slate",
});

function Choice({
  label,
  hint,
  required,
  options,
  secondary,
  value,
  onChange,
}: {
  label: string;
  hint: string;
  required?: boolean;
  options: ChoiceOption[];
  secondary?: { caption: string; options: ChoiceOption[] };
  value: string | null;
  onChange: (value: string) => void;
}) {
  const t = useTranslation();

  const card = (option: ChoiceOption) => {
    const selected = value === option.value;
    return (
      <button
        key={option.value}
        type="button"
        onClick={() => onChange(option.value)}
        aria-pressed={selected}
        className={cn(
          "flex flex-col gap-1 rounded-lg border px-3 py-2.5 text-left transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-600",
          selected
            ? TONES[option.tone]
            : "border-slate-200 text-slate-600 hover:bg-slate-50"
        )}
      >
        <span className="flex items-center gap-1.5 text-sm font-medium">
          {option.icon}
          {t(option.labelKey)}
        </span>
        <span className="text-xs opacity-80">{t(option.hintKey)}</span>
      </button>
    );
  };

  return (
    <div className="space-y-1.5">
      <FieldLabel required={required}>{label}</FieldLabel>
      <p className="text-xs text-slate-500">{hint}</p>
      <div className="grid grid-cols-2 gap-2 pt-0.5">
        {options.map(card)}
      </div>
      {secondary && (
        <div className="space-y-1.5 border-t border-dashed border-slate-200 pt-2.5">
          <p className="text-xs text-slate-400">{secondary.caption}</p>
          <div className="grid grid-cols-2 gap-2">
            {secondary.options.map(card)}
          </div>
        </div>
      )}
    </div>
  );
}
