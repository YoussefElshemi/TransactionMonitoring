import assert from "node:assert/strict";
import test from "node:test";
import { readFileSync, readdirSync } from "node:fs";
import { fileURLToPath } from "node:url";
import path from "node:path";

const ROOT = path.resolve(fileURLToPath(new URL("../app", import.meta.url)));

const walk = (dir, extension = ".tsx") =>
  readdirSync(dir, { withFileTypes: true }).flatMap((entry) => {
    const full = path.join(dir, entry.name);
    if (entry.isDirectory()) return walk(full, extension);
    return entry.name.endsWith(extension) ? [full] : [];
  });

const allSources = (dir) => [...walk(dir, ".tsx"), ...walk(dir, ".ts")];

const rel = (file) => path.relative(ROOT, file).replace(/\\/g, "/");

const EXEMPT = new Set([
  "status/page.tsx",
  "status/layout.tsx",
  "status/subscriptions/[statusSubscriptionId]/confirm/page.tsx",
  "status/subscriptions/[statusSubscriptionId]/unsubscribe/page.tsx",
  "components/status/SubscriptionLanding.tsx",
  "components/status/UptimeBar.tsx",
]);

const EXEMPT_DIRECTORIES = [
  "docs/",
  "components/docs/",
];

const isExempt = (name) =>
  EXEMPT.has(name) || EXEMPT_DIRECTORIES.some((directory) => name.startsWith(directory));

const TEXT_PROPS = [
  "title", "label", "placeholder", "description", "eyebrow",
  "emptyLabel", "clearLabel", "backLabel", "aria-label", "confirmLabel",
];

/**
 * A JSX text node.
 *
 * The leading `[^=\-<]` is load-bearing and was learnt the hard way: without it
 * an arrow type — `(id: string) => Promise<void>` — reads as a text node holding
 * "Promise", and the extraction codemod duly put a translation call inside a
 * type annotation.
 */
const JSX_TEXT = /([^=\-<])>\s*([A-Z][A-Za-z0-9][^<>{}\n]{2,})\s*</g;

const MIXED_TEXT = /([^=\-<])>([^<>\n]*\{[^<>\n]*)</g;

const proseOutsideInterpolations = (text) => {
  let depth = 0;
  let outside = "";

  for (const character of text) {
    if (character === "{") depth++;
    else if (character === "}") {
      if (--depth < 0) return null;
    } else if (depth === 0) outside += character;
  }

  return depth === 0 ? outside : null;
};
const PROP_TEXT = new RegExp(`\\b(?:${TEXT_PROPS.join("|")})="([^"]{3,})"`, "g");

const isFormatExample = (text) =>
  /^[\d\s.,%:+\-/]+$/.test(text) ||
  /^[A-Z]{2,}-?\d+$/.test(text) ||
  /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(text);

const offenders = [];

for (const file of walk(ROOT)) {
  const name = rel(file);
  if (isExempt(name)) continue;

  const src = readFileSync(file, "utf8");

  for (const m of src.matchAll(JSX_TEXT)) {
    const text = m[2].trim();
    if (/^https?:/.test(text) || isFormatExample(text)) continue;
    offenders.push({ name, text });
  }
  for (const m of src.matchAll(PROP_TEXT)) {
    const text = m[1].trim();
    if (isFormatExample(text)) continue;
    offenders.push({ name, text });
  }
  for (const m of src.matchAll(MIXED_TEXT)) {
    const outside = proseOutsideInterpolations(m[2]);
    if (outside === null) continue;
    if (!/[A-Za-z]{2,}/.test(outside)) continue;
    offenders.push({ name, text: m[2].trim() });
  }
}

test("no user-facing string literal is left outside the catalogue", () => {
  const shown = offenders
    .slice(0, 20)
    .map((o) => `  ${o.name}: ${JSON.stringify(o.text)}`)
    .join("\n");

  assert.equal(
    offenders.length,
    0,
    `${offenders.length} literal(s) not going through useTranslation():\n${shown}\n\n` +
      "Add the string to app/messages/en.json and render it with t(\"key\"). " +
      "If it is text a *user* wrote — a case name, a note, a reason code — it " +
      "must not be translated at all, and it should not be a literal in the JSX."
  );
});

test("every catalogue key is actually used", () => {
  const catalogue = JSON.parse(
    readFileSync(new URL("../app/messages/en.json", import.meta.url), "utf8")
  );

  const sources = allSources(ROOT)
    .map((f) => readFileSync(f, "utf8"))
    .join("\n");

  const COMPUTED_PREFIXES = [
    "severity.",
    "status.",
    "attachments.scanStatus.",
  ];

  const unused = Object.keys(catalogue).filter(
    (key) =>
      !sources.includes(`"${key}"`) &&
      !COMPUTED_PREFIXES.some((prefix) => key.startsWith(prefix))
  );

  assert.deepEqual(
    unused,
    [],
    `${unused.length} catalogue key(s) are never rendered`
  );
});

test("every catalogue value is a non-empty string with balanced placeholders", () => {
  const catalogue = JSON.parse(
    readFileSync(new URL("../app/messages/en.json", import.meta.url), "utf8")
  );

  for (const [key, message] of Object.entries(catalogue)) {
    assert.equal(typeof message, "string", `${key} is not a string`);
    assert.ok(message.length > 0, `${key} is empty`);

    const opens = (message.match(/\{/g) ?? []).length;
    const placeholders = (message.match(/\{\d+\}/g) ?? []).length;
    assert.equal(opens, placeholders, `${key} has a malformed placeholder`);
  }
});

test("the other languages hold only keys English also has", () => {
  const english = JSON.parse(
    readFileSync(new URL("../app/messages/en.json", import.meta.url), "utf8")
  );

  for (const language of ["fr", "de", "es", "nl"]) {
    const catalogue = JSON.parse(
      readFileSync(
        new URL(`../app/messages/${language}.json`, import.meta.url),
        "utf8"
      )
    );

    const orphans = Object.keys(catalogue).filter((key) => !(key in english));
    assert.deepEqual(
      orphans,
      [],
      `${language}.json holds ${orphans.length} key(s) English does not`
    );
  }
});
