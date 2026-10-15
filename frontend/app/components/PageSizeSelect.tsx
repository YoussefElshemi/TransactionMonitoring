"use client";

import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";

import { useTranslation } from "@/context/LocalizationContext";
import { useFormat } from "@/lib/useFormat";

const PAGE_SIZES = [10, 25, 50, 100] as const;

interface PageSizeSelectProps {
  value: number;
  onChange: (size: number) => void;
  className?: string;
}

export function PageSizeSelect({
  value,
  onChange,
  className = "w-36",
}: PageSizeSelectProps) {
  const t = useTranslation();
  const format = useFormat();

  return (
    <Select
      value={String(value)}
      onValueChange={(next) => onChange(parseInt(next))}
    >
      <SelectTrigger className={className} aria-label={t("pageSize.label")}>
        <SelectValue />
      </SelectTrigger>
      <SelectContent>
        {PAGE_SIZES.map((size) => (
          <SelectItem key={size} value={String(size)}>
            {t("pageSize.option", format.count(size))}
          </SelectItem>
        ))}
      </SelectContent>
    </Select>
  );
}

export default PageSizeSelect;
