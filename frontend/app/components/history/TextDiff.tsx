"use client";

import { useMemo, useState } from "react";

import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";
import { useTranslation } from "@/context/LocalizationContext";

type LineType = "context" | "added" | "removed";

interface DiffLine {
  type: LineType;
  text: string;
}

// A full LCS table is O(before × after). Rule bodies are small, but the trail
// also carries free text, so bail out to a plain before/after listing rather
// than locking the tab up on a pathological value.
const MAX_DIFFABLE_LINES = 400;

const diffLines = (before: string[], after: string[]): DiffLine[] => {
  const lengths: number[][] = Array.from({ length: before.length + 1 }, () =>
    new Array<number>(after.length + 1).fill(0)
  );

  for (let i = before.length - 1; i >= 0; i--) {
    for (let j = after.length - 1; j >= 0; j--) {
      lengths[i][j] =
        before[i] === after[j]
          ? lengths[i + 1][j + 1] + 1
          : Math.max(lengths[i + 1][j], lengths[i][j + 1]);
    }
  }

  const lines: DiffLine[] = [];
  let i = 0;
  let j = 0;
  while (i < before.length && j < after.length) {
    if (before[i] === after[j]) {
      lines.push({ type: "context", text: before[i] });
      i++;
      j++;
    } else if (lengths[i + 1][j] >= lengths[i][j + 1]) {
      lines.push({ type: "removed", text: before[i] });
      i++;
    } else {
      lines.push({ type: "added", text: after[j] });
      j++;
    }
  }
  while (i < before.length) lines.push({ type: "removed", text: before[i++] });
  while (j < after.length) lines.push({ type: "added", text: after[j++] });

  return lines;
};

const LINE_STYLES: Record<LineType, string> = {
  context: "text-slate-500",
  added: "bg-settled-50 text-settled-700",
  removed: "bg-critical-50 text-critical-700",
};

const GUTTER: Record<LineType, string> = {
  context: " ",
  added: "+",
  removed: "-",
};

interface TextDiffProps {
  before: string | null;
  after: string | null;
  previewLines?: number;
  className?: string;
}

export function TextDiff({
  before,
  after,
  previewLines = 6,
  className,
}: TextDiffProps) {
  const t = useTranslation();
  const [expanded, setExpanded] = useState<boolean>(false);

  const { lines, tooLarge } = useMemo(() => {
    const beforeLines = before === null ? [] : before.split("\n");
    const afterLines = after === null ? [] : after.split("\n");

    if (
      beforeLines.length > MAX_DIFFABLE_LINES ||
      afterLines.length > MAX_DIFFABLE_LINES
    ) {
      return {
        tooLarge: true,
        lines: [
          ...beforeLines.map<DiffLine>((text) => ({ type: "removed", text })),
          ...afterLines.map<DiffLine>((text) => ({ type: "added", text })),
        ],
      };
    }

    return { tooLarge: false, lines: diffLines(beforeLines, afterLines) };
  }, [before, after]);

  const collapsible = lines.length > previewLines;
  const visible = expanded || !collapsible ? lines : lines.slice(0, previewLines);

  return (
    <div className={cn("space-y-1.5", className)}>
      <pre className="overflow-x-auto rounded-md border border-slate-200 bg-slate-50/60 py-1.5 text-xs leading-5">
        {visible.map((line, index) => (
          <div
            key={index}
            className={cn("px-3 font-mono", LINE_STYLES[line.type])}
          >
            <span className="mr-2 select-none text-slate-300">
              {GUTTER[line.type]}
            </span>
            {line.text || " "}
          </div>
        ))}
      </pre>

      <div className="flex items-center gap-3">
        {collapsible && (
          <Button
            variant="ghost"
            size="sm"
            className="h-auto px-0 text-xs text-brand-800 hover:bg-transparent hover:text-brand-900"
            onClick={() => setExpanded((open) => !open)}
          >
            {expanded
              ? "Collapse"
              : `Show all ${lines.length} lines`}
          </Button>
        )}
        {tooLarge && (
          <span className="text-xs text-slate-400">
            {t("history.valueTooLargeToDiff")}
          </span>
        )}
      </div>
    </div>
  );
}
