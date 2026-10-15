"use client";

import { useEffect, useRef, useState, type KeyboardEvent } from "react";
import { Check, ChevronDown } from "lucide-react";

import { ApplicationUser } from "@/types";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";

import { useTranslation } from "@/context/LocalizationContext";

const initials = (candidate: ApplicationUser) => {
  const source = candidate.userName || candidate.email || "";
  const words = source.split(/[\s._-]+/).filter(Boolean);

  if (words.length >= 2) {
    return (words[0][0] + words[1][0]).toUpperCase();
  }

  return source.slice(0, 2).toUpperCase();
};

const roleLabel = (candidate: ApplicationUser) => {
  if (candidate.roles?.includes("Admin")) return "admin";
  if (candidate.roles?.includes("Auditor")) return "auditor";
  if (candidate.roles?.includes("Manager")) return "manager";
  return null;
};

export interface UserComboboxProps {
  users: ApplicationUser[];
  loading: boolean;
  value: ApplicationUser | null;
  onChange: (user: ApplicationUser | null) => void;
  onSearchTermChange: (term: string) => void;
  onSubmit?: () => void;
  disabled?: boolean;
  placeholder?: string;
  emptyLabel?: string;
}

export function UserCombobox({
  users,
  loading,
  value,
  onChange,
  onSearchTermChange,
  onSubmit,
  disabled = false,
  placeholder,
  emptyLabel,
}: UserComboboxProps) {
  const t = useTranslation();
  const [query, setQuery] = useState("");
  const [open, setOpen] = useState(false);
  const [highlight, setHighlight] = useState(-1);
  const rowRefs = useRef<Record<string, HTMLLIElement | null>>({});

  const searchTerm = value && query === value.userName ? "" : query;

  // Held in a ref so an inline arrow function from the caller doesn't restart
  // the debounce on every render.
  const notifySearch = useRef(onSearchTermChange);
  useEffect(() => {
    notifySearch.current = onSearchTermChange;
  });

  useEffect(() => {
    const timer = setTimeout(
      () => notifySearch.current(searchTerm),
      searchTerm ? 300 : 0
    );
    return () => clearTimeout(timer);
  }, [searchTerm]);

  useEffect(() => setHighlight(-1), [users]);

  const pick = (candidate: ApplicationUser) => {
    onChange(candidate);
    setQuery(candidate.userName);
    setOpen(false);
    setHighlight(-1);
  };

  const handleKeyDown = (event: KeyboardEvent) => {
    if (event.key === "Escape" && open) {
      event.preventDefault();
      setOpen(false);
      return;
    }

    if (event.key === "Enter") {
      if (open && highlight >= 0 && users[highlight]) {
        event.preventDefault();
        pick(users[highlight]);
      } else if (value && onSubmit) {
        event.preventDefault();
        onSubmit();
      }
      return;
    }

    if (event.key !== "ArrowDown" && event.key !== "ArrowUp") return;
    if (!users.length) return;

    event.preventDefault();
    setOpen(true);

    const step = event.key === "ArrowDown" ? 1 : -1;
    const next =
      highlight === -1
        ? step === 1
          ? 0
          : users.length - 1
        : (highlight + step + users.length) % users.length;

    setHighlight(next);
    rowRefs.current[users[next].userId]?.scrollIntoView({ block: "nearest" });
  };

  return (
    <div className="relative">
      <Input
        placeholder={placeholder ?? t("userCombobox.placeholder")}
        value={query}
        disabled={disabled}
        onChange={(e) => {
          setQuery(e.target.value);
          onChange(null);
          setOpen(true);
        }}
        onFocus={() => setOpen(true)}
        onBlur={() => setTimeout(() => setOpen(false), 200)}
        onKeyDown={handleKeyDown}
        className="pr-9"
      />
      <ChevronDown className="pointer-events-none absolute right-3 top-1/2 size-4 -translate-y-1/2 text-slate-400" />

      {open && (
        <ul className="absolute left-0 right-0 z-10 mt-1 max-h-56 overflow-y-auto rounded-md border border-slate-200 bg-card py-1 shadow-lg">
          {loading ? (
            <li className="space-y-1 px-2 py-1">
              {Array.from({ length: 3 }).map((_, i) => (
                <Skeleton key={i} className="h-9 w-full" />
              ))}
            </li>
          ) : !users.length ? (
            <li className="px-3 py-6 text-center text-sm text-slate-500">
              {searchTerm
                ? t("userCombobox.noMatches", searchTerm)
                : (emptyLabel ?? t("userCombobox.empty"))}
            </li>
          ) : (
            users.map((candidate, index) => {
              const isSelected = value?.userId === candidate.userId;
              const role = roleLabel(candidate);

              return (
                <li
                  key={candidate.userId}
                  ref={(node) => {
                    rowRefs.current[candidate.userId] = node;
                  }}
                  // Fires ahead of the input's blur, so the pick lands before
                  // the list is torn down.
                  onMouseDown={() => pick(candidate)}
                  onMouseEnter={() => setHighlight(index)}
                  className={`flex cursor-pointer items-center gap-3 px-3 py-2 ${
                    index === highlight ? "bg-slate-100" : ""
                  }`}
                >
                  <span className="flex size-8 flex-shrink-0 items-center justify-center rounded-full bg-slate-100 font-mono text-[11px] text-slate-500">
                    {initials(candidate)}
                  </span>
                  <span className="min-w-0 flex-1">
                    <span className="flex items-center gap-1.5">
                      <span className="truncate text-sm text-slate-900">
                        {candidate.userName}
                      </span>
                      {role && (
                        <span className="flex-shrink-0 rounded bg-slate-100 px-1.5 py-0.5 font-mono text-[10px] uppercase tracking-wider text-slate-500">
                          {role}
                        </span>
                      )}
                    </span>
                    <span className="block truncate text-xs text-slate-500">
                      {candidate.email}
                    </span>
                  </span>
                  {isSelected && (
                    <Check className="size-4 flex-shrink-0 text-brand-800" />
                  )}
                </li>
              );
            })
          )}
        </ul>
      )}
    </div>
  );
}
