"use client";

import { useMemo, useRef, useState, type KeyboardEvent } from "react";
import { Check, TriangleAlert } from "lucide-react";

import { SchemaField } from "@/types";
import { Input } from "@/components/ui/input";
import { SchemaPath, schemaPaths } from "@/lib/transactionSchema";
import { cn } from "@/lib/utils";

import { useTranslation } from "@/context/LocalizationContext";
import { useFormat } from "@/lib/useFormat";

export function SchemaFieldCombobox({
  value,
  onChange,
  disabled = false,
  fields,
  tone = "default",
  className,
  placeholder,
  filter,
  emptyNote,
  paths: options,
}: {
  value: string;
  onChange: (value: string) => void;
  disabled?: boolean;
  fields: SchemaField[] | null;
  tone?: "default" | "warning" | "critical";
  className?: string;
  placeholder?: string;
  filter?: (path: SchemaPath) => boolean;
  emptyNote?: string;
  paths?: SchemaPath[];
}) {
  const t = useTranslation();
  const format = useFormat();
  const [open, setOpen] = useState(false);
  const [highlight, setHighlight] = useState(-1);
  const rowRefs = useRef<Record<string, HTMLLIElement | null>>({});

  const paths = useMemo(() => {
    if (!fields) return [];
    const all = options ?? schemaPaths(fields);
    return filter ? all.filter(filter) : all;
  }, [fields, options, filter]);

  const needle = value.trim().toLowerCase();
  const matches = paths.filter((path) =>
    path.path.toLowerCase().includes(needle)
  );

  const nothingFits = fields !== null && paths.length === 0;

  const pick = (path: SchemaPath) => {
    onChange(path.path);
    setOpen(false);
    setHighlight(-1);
  };

  const handleKeyDown = (event: KeyboardEvent) => {
    if (event.key === "Escape" && open) {
      event.preventDefault();
      setOpen(false);
      return;
    }

    if (event.key === "Enter" && open && highlight >= 0 && matches[highlight]) {
      event.preventDefault();
      pick(matches[highlight]);
      return;
    }

    if (event.key !== "ArrowDown" && event.key !== "ArrowUp") return;
    if (!matches.length) return;

    event.preventDefault();
    setOpen(true);

    const step = event.key === "ArrowDown" ? 1 : -1;
    const next =
      highlight === -1
        ? step === 1
          ? 0
          : matches.length - 1
        : (highlight + step + matches.length) % matches.length;

    setHighlight(next);
    rowRefs.current[matches[next].path]?.scrollIntoView({ block: "nearest" });
  };

  return (
    <div className={cn("relative", className)}>
      <Input
        placeholder={placeholder ?? t("schemaField.placeholder")}
        value={value}
        disabled={disabled}
        onChange={(event) => {
          onChange(event.target.value);
          setOpen(true);
          setHighlight(-1);
        }}
        onFocus={() => setOpen(true)}
        onBlur={() => setTimeout(() => setOpen(false), 200)}
        onKeyDown={handleKeyDown}
        className={cn(
          "w-full font-mono",
          tone === "critical" && "border-critical-500",
          tone === "warning" && "border-attention-500",
          tone === "default" && nothingFits && emptyNote && "border-attention-500"
        )}
      />

      {open && fields !== null && matches.length > 0 && (
        <ul className="absolute left-0 z-20 mt-1 max-h-56 w-max min-w-full max-w-[32rem] overflow-y-auto rounded-md border border-slate-200 bg-card py-1 shadow-lg">
          {matches.map((path, index) => (
            <li
              key={path.path}
              ref={(node) => {
                rowRefs.current[path.path] = node;
              }}
              // Ahead of the input's blur, so the pick lands before the list
              // is torn down.
              onMouseDown={() => pick(path)}
              onMouseEnter={() => setHighlight(index)}
              className={cn(
                "flex cursor-pointer items-center gap-2 px-3 py-1.5",
                index === highlight && "bg-slate-100"
              )}
            >
              <span
                className="min-w-0 flex-1"
                style={{ paddingLeft: `${path.depth * 0.6}rem` }}
              >
                <span className="block truncate font-mono text-sm text-slate-900">
                  {path.path}
                </span>
                <span className="block truncate text-xs text-slate-500">
                  {path.type}
                  {path.multiple && t("schemaField.many")}
                  {path.isRequired && t("schemaField.required")}
                  {path.allowedValues?.length
                    ? t(
                        path.allowedValues.length === 1
                          ? "schemaField.allowedValue"
                          : "schemaField.allowedValues",
                        format.count(path.allowedValues.length)
                      )
                    : ""}
                </span>
              </span>
              {path.path === value && (
                <Check className="size-4 flex-shrink-0 text-brand-800" />
              )}
            </li>
          ))}
        </ul>
      )}

      {nothingFits && emptyNote && (
        <p className="mt-1 flex items-start gap-1.5 text-xs text-attention-700">
          <TriangleAlert className="mt-px size-3.5 flex-shrink-0" />
          <span>{emptyNote}</span>
        </p>
      )}
    </div>
  );
}
