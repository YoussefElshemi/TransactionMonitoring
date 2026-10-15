"use client";

import { useEffect, useId, useMemo, useRef, useState } from "react";
import { Check, ChevronDown, X } from "lucide-react";

import { useTranslation } from "@/context/LocalizationContext";
import { Input } from "@/components/ui/input";
import { cn } from "@/lib/utils";

export function InheritedSelect({
  label,
  help,
  value,
  inheritedValue,
  inheritedFrom,
  options,
  optionLabel,
  onChange,
  disabled = false,
}: {
  label: string;
  help?: string;
  value: string | null;
  inheritedValue: string;
  inheritedFrom: string;
  options: string[];
  optionLabel: (option: string) => string;
  onChange: (next: string | null) => void;
  disabled?: boolean;
}) {
  const t = useTranslation();
  const fieldId = useId();
  const listId = `${fieldId}-list`;
  const helpId = `${fieldId}-help`;
  const stateId = `${fieldId}-state`;

  const [open, setOpen] = useState(false);
  const [query, setQuery] = useState("");
  const [highlighted, setHighlighted] = useState(0);
  const container = useRef<HTMLDivElement>(null);
  const list = useRef<HTMLUListElement>(null);

  const matches = useMemo(() => {
    const needle = query.trim().toLowerCase();
    const all = needle
      ? options.filter(
          (option) =>
            option.toLowerCase().includes(needle) ||
            optionLabel(option).toLowerCase().includes(needle)
        )
      : options;

    return all.slice(0, 50);
  }, [options, optionLabel, query]);

  useEffect(() => {
    if (!open) return;

    const onPointerDown = (event: PointerEvent) => {
      if (!container.current?.contains(event.target as Node)) setOpen(false);
    };

    document.addEventListener("pointerdown", onPointerDown);
    return () => document.removeEventListener("pointerdown", onPointerDown);
  }, [open]);

  useEffect(() => {
    if (!open) return;
    list.current
      ?.querySelector(`[data-index="${highlighted}"]`)
      ?.scrollIntoView({ block: "nearest" });
  }, [highlighted, open]);

  const commit = (option: string) => {
    onChange(option);
    setQuery("");
    setOpen(false);
  };

  const onKeyDown = (event: React.KeyboardEvent<HTMLInputElement>) => {
    if (event.key === "ArrowDown" || event.key === "ArrowUp") {
      event.preventDefault();
      if (!open) {
        setOpen(true);
        setHighlighted(0);
        return;
      }
      const delta = event.key === "ArrowDown" ? 1 : -1;
      setHighlighted((current) => {
        if (matches.length === 0) return 0;
        return (current + delta + matches.length) % matches.length;
      });
      return;
    }

    if (event.key === "Enter" && open && matches[highlighted]) {
      event.preventDefault();
      commit(matches[highlighted]);
      return;
    }

    if (event.key === "Escape") {
      // Not `preventDefault` unconditionally: with the list shut, Escape
      // belongs to whatever is above this — a dialog that should close.
      if (open || query) {
        event.preventDefault();
        setOpen(false);
        setQuery("");
      }
    }
  };

  const stated = value !== null;
  const resting = stated
    ? optionLabel(value)
    : t("preferences.inherited.value", optionLabel(inheritedValue));

  return (
    <div className="space-y-1.5" ref={container}>
      <label
        htmlFor={fieldId}
        className="block font-mono text-xs uppercase tracking-widest text-slate-400"
      >
        {label}
      </label>

      <div className="relative">
        <Input
          id={fieldId}
          role="combobox"
          aria-expanded={open}
          aria-controls={open ? listId : undefined}
          aria-autocomplete="list"
          aria-activedescendant={
            open && matches[highlighted]
              ? `${fieldId}-option-${highlighted}`
              : undefined
          }
          aria-describedby={cn(help && helpId, stateId)}
          autoComplete="off"
          disabled={disabled}
          value={open ? query : resting}
          placeholder={resting}
          onFocus={() => setOpen(true)}
          onChange={(event) => {
            setQuery(event.target.value);
            setHighlighted(0);
            setOpen(true);
          }}
          onKeyDown={onKeyDown}
          className={cn("pr-9", !stated && !open && "text-slate-500")}
        />
        <ChevronDown
          aria-hidden
          className="pointer-events-none absolute right-3 top-1/2 size-4 -translate-y-1/2 text-slate-400"
        />

        {open && (
          <ul
            ref={list}
            id={listId}
            role="listbox"
            aria-label={label}
            className="absolute z-50 mt-1 max-h-64 w-full overflow-y-auto rounded-md border border-slate-200 bg-popover py-1 shadow-lg"
          >
            {matches.length === 0 ? (
              <li className="px-3 py-2 text-sm text-slate-500">
                {t("preferences.noMatches")}
              </li>
            ) : (
              matches.map((option, index) => (
                <li
                  key={option}
                  id={`${fieldId}-option-${index}`}
                  role="option"
                  data-index={index}
                  aria-selected={option === value}
                  // `onPointerDown` rather than `onClick`: the input's blur
                  // would otherwise close the list before the click landed.
                  onPointerDown={(event) => {
                    event.preventDefault();
                    commit(option);
                  }}
                  onMouseEnter={() => setHighlighted(index)}
                  className={cn(
                    "flex cursor-pointer items-center gap-2 px-3 py-1.5 font-mono text-sm",
                    index === highlighted
                      ? "bg-accent text-accent-foreground"
                      : "text-slate-700"
                  )}
                >
                  <Check
                    aria-hidden
                    className={cn(
                      "size-3.5 flex-shrink-0",
                      option === value ? "opacity-100" : "opacity-0"
                    )}
                  />
                  <span className="truncate">{optionLabel(option)}</span>
                </li>
              ))
            )}
          </ul>
        )}
      </div>

      {help && (
        <p id={helpId} className="text-xs leading-relaxed text-slate-500">
          {help}
        </p>
      )}

      <p
        id={stateId}
        aria-live="polite"
        className="flex flex-wrap items-center gap-x-2 gap-y-1 text-xs"
      >
        {stated ? (
          <>
            <span className="text-slate-500">{t("preferences.stated")}</span>
            <button
              type="button"
              disabled={disabled}
              onClick={() => onChange(null)}
              className="inline-flex items-center gap-1 rounded text-brand-800 underline-offset-2 hover:underline focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:opacity-50"
            >
              <X aria-hidden className="size-3" />
              {t("preferences.clear")}
            </button>
          </>
        ) : (
          <span className="text-slate-500">{inheritedFrom}</span>
        )}
      </p>
    </div>
  );
}
