"use client";

import { CaseDisposition } from "@/types";
import {
  DISPOSITION_HINTS,
  PRIMARY_DISPOSITIONS,
  SECONDARY_DISPOSITIONS,
  dispositionLabelKey,
} from "@/lib/cases";
import {
  Select,
  SelectContent,
  SelectGroup,
  SelectItem,
  SelectLabel,
  SelectSeparator,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { useTranslation } from "@/context/LocalizationContext";

export const DISPOSITION_ANY = "All";

export function DispositionSelect({
  value,
  onChange,
  disabled,
}: {
  value: string;
  onChange: (value: string) => void;
  disabled?: boolean;
}) {
  const t = useTranslation();
  return (
    <div className="space-y-1.5">
      <label className="font-mono text-xs uppercase tracking-widest text-slate-500">
        {t("reasonCodes.appliesTo")}
      </label>
      <Select value={value} onValueChange={onChange} disabled={disabled}>
        <SelectTrigger>
          <SelectValue />
        </SelectTrigger>
        <SelectContent>
          <SelectItem value={DISPOSITION_ANY}>{t("reasonCodes.allConclusions")}</SelectItem>
          <SelectSeparator />
          <SelectGroup>
            <SelectLabel>{t("reasonCodes.judgesTheAlert")}</SelectLabel>
            {PRIMARY_DISPOSITIONS.map((disposition) => (
              <SelectItem key={disposition} value={disposition}>
                {t(
                  "reasonCodes.dispositionOnly",
                  t(dispositionLabelKey(disposition))
                )}
              </SelectItem>
            ))}
          </SelectGroup>
          <SelectSeparator />
          <SelectGroup>
            <SelectLabel>{t("reasonCodes.judgesWhatWasExaminedBeside")}</SelectLabel>
            {SECONDARY_DISPOSITIONS.map((disposition) => (
              <SelectItem key={disposition} value={disposition}>
                {t(
                  "reasonCodes.dispositionOnly",
                  t(dispositionLabelKey(disposition))
                )}
              </SelectItem>
            ))}
          </SelectGroup>
        </SelectContent>
      </Select>

      <p className="text-xs text-slate-600">
        {value === DISPOSITION_ANY
          ? t("reasonCodes.offeredWhicheverConclusion")
          : t(DISPOSITION_HINTS[value as CaseDisposition])}
      </p>

      <p className="text-xs text-slate-500">
        A bound code is offered only when the analyst picks that conclusion, and
        a close naming one bound to a different conclusion is refused. Leave it
        on all conclusions for the codes that read either way — &ldquo;customer
        confirmed it&rdquo; being the usual one.
      </p>
    </div>
  );
}
