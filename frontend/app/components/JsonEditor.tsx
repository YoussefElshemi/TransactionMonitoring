"use client";

import { useId, useMemo, useRef } from "react";
import CodeMirror, {
  EditorView,
  Prec,
  keymap,
  type ReactCodeMirrorProps,
} from "@uiw/react-codemirror";
import { json } from "@codemirror/lang-json";
import { HighlightStyle, syntaxHighlighting } from "@codemirror/language";
import { tags } from "@lezer/highlight";

import { useTheme } from "@/context/ThemeContext";
import { useTranslation } from "@/context/LocalizationContext";
import { cn } from "@/lib/utils";

const themeSpec = {
  "&": {
    backgroundColor: "hsl(var(--card))",
    color: "hsl(var(--slate-900))",
    fontSize: "13px",
  },
  "&.cm-focused": { outline: "none" },
  ".cm-content": {
    fontFamily: "var(--font-geist-mono), ui-monospace, monospace",
    caretColor: "hsl(var(--brand-800))",
  },
  ".cm-cursor, .cm-dropCursor": {
    borderLeftColor: "hsl(var(--brand-800))",
  },
  // CodeMirror swaps which of these it uses depending on focus; both need
  // setting or the selection vanishes the moment the editor loses focus.
  "&.cm-focused .cm-selectionBackground, .cm-selectionBackground, .cm-content ::selection":
    { backgroundColor: "hsl(var(--brand-200))" },
  ".cm-activeLine": { backgroundColor: "hsl(var(--slate-50))" },
  ".cm-gutters": {
    backgroundColor: "transparent",
    color: "hsl(var(--slate-400))",
    border: "none",
  },
  ".cm-activeLineGutter": {
    backgroundColor: "hsl(var(--slate-50))",
    color: "hsl(var(--slate-500))",
  },
  ".cm-foldPlaceholder": {
    backgroundColor: "hsl(var(--slate-100))",
    color: "hsl(var(--slate-500))",
    border: "none",
  },
  ".cm-matchingBracket, .cm-nonmatchingBracket": {
    backgroundColor: "hsl(var(--brand-100))",
    outline: "none",
  },
};

const highlightStyle = HighlightStyle.define([
  { tag: [tags.propertyName, tags.definition(tags.propertyName)], color: "hsl(var(--brand-800))" },
  { tag: [tags.string, tags.special(tags.string)], color: "hsl(var(--settled-700))" },
  { tag: [tags.number, tags.integer, tags.float], color: "hsl(var(--elevated-700))" },
  { tag: [tags.bool, tags.null, tags.atom, tags.keyword], color: "hsl(var(--critical-700))" },
  { tag: [tags.punctuation, tags.separator, tags.bracket], color: "hsl(var(--slate-500))" },
  { tag: tags.invalid, color: "hsl(var(--critical-700))" },
  { tag: tags.comment, color: "hsl(var(--slate-400))", fontStyle: "italic" },
]);

const escapeReleasesFocus = (wrapper: React.RefObject<HTMLDivElement | null>) =>
  Prec.highest(
    keymap.of([
      {
        key: "Escape",
        run: () => {
          wrapper.current?.focus();
          return true;
        },
      },
    ])
  );

type JsonEditorProps = Omit<ReactCodeMirrorProps, "extensions" | "theme"> & {
  className?: string;
};

export function JsonEditor({ className, ...props }: JsonEditorProps) {
  const { theme } = useTheme();
  const t = useTranslation();
  const wrapper = useRef<HTMLDivElement>(null);
  const hintId = useId();

  const extensions = useMemo(
    () => [
      json(),
      escapeReleasesFocus(wrapper),
      EditorView.theme(themeSpec, { dark: theme === "dark" }),
      syntaxHighlighting(highlightStyle),
    ],
    [theme]
  );

  return (
    <div ref={wrapper} tabIndex={-1} className="focus:outline-none">
      <CodeMirror
        theme="none"
        extensions={extensions}
        className={cn("[&_.cm-editor]:bg-transparent", className)}
        aria-describedby={hintId}
        {...props}
      />
      <p
        id={hintId}
        className="mt-1.5 font-mono text-[11px] text-slate-400"
      >
        {t("rules.editor.escapeHint")}
      </p>
    </div>
  );
}
