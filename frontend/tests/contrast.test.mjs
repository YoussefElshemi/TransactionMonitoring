import assert from "node:assert/strict";
import test from "node:test";
import { readFileSync } from "node:fs";

const css = readFileSync(new URL("../app/globals.css", import.meta.url), "utf8");

const block = (selector) => {
  const start = css.indexOf(selector);
  assert.notEqual(start, -1, `${selector} not found in globals.css`);

  const open = css.indexOf("{", start);
  let depth = 0;
  let i = open;
  for (; i < css.length; i++) {
    if (css[i] === "{") depth++;
    else if (css[i] === "}" && --depth === 0) break;
  }
  return css.slice(open, i);
};

const parse = (text) =>
  Object.fromEntries(
    [...text.matchAll(/--([\w-]+):\s*([^;]+);/g)].map((m) => [m[1], m[2].trim()])
  );

const light = parse(block(":root"));
const dark = { ...light, ...parse(block(".dark")) };

const resolve = (vars, value, depth = 0) => {
  assert.ok(depth < 10, "token alias loop");
  const alias = value?.match(/^var\(--([\w-]+)\)$/);
  return alias ? resolve(vars, vars[alias[1]], depth + 1) : value;
};

const hslToRgb = (spec) => {
  const m = spec?.match(/^([\d.]+)\s+([\d.]+)%\s+([\d.]+)%$/);
  assert.ok(m, `not an HSL triple: ${spec}`);

  const h = +m[1] / 360;
  const s = +m[2] / 100;
  const l = +m[3] / 100;
  if (s === 0) return [l * 255, l * 255, l * 255];

  const q = l < 0.5 ? l * (1 + s) : l + s - l * s;
  const p = 2 * l - q;
  const channel = (t) => {
    if (t < 0) t += 1;
    if (t > 1) t -= 1;
    if (t < 1 / 6) return p + (q - p) * 6 * t;
    if (t < 1 / 2) return q;
    if (t < 2 / 3) return p + (q - p) * (2 / 3 - t) * 6;
    return p;
  };

  return [channel(h + 1 / 3), channel(h), channel(h - 1 / 3)].map((v) => v * 255);
};

const luminance = (rgb) => {
  const [r, g, b] = rgb.map((v) => {
    const c = v / 255;
    return c <= 0.03928 ? c / 12.92 : ((c + 0.055) / 1.055) ** 2.4;
  });
  return 0.2126 * r + 0.7152 * g + 0.0722 * b;
};

const ratio = (vars, foreground, background) => {
  const [a, b] = [foreground, background].map((token) =>
    luminance(hslToRgb(resolve(vars, vars[token])))
  );
  const [lighter, darker] = a > b ? [a, b] : [b, a];
  return (lighter + 0.05) / (darker + 0.05);
};

const PAIRS = [
  ["severity Info", "slate-600", "slate-100"],
  ["severity Low", "settled-700", "settled-50"],
  ["severity Medium", "attention-700", "attention-50"],
  ["severity High", "elevated-700", "elevated-50"],
  ["severity Critical", "critical-700", "critical-50"],

  ["severity Info (emphasis)", "slate-600", "slate-50"],
  ["severity Low (emphasis)", "settled-700", "settled-50"],
  ["severity Medium (emphasis)", "attention-700", "attention-50"],
  ["severity High (emphasis)", "elevated-700", "elevated-50"],
  ["severity Critical (emphasis)", "critical-solid-foreground", "critical-solid"],

  ["decision Held", "attention-700", "attention-50"],
  ["decision Released", "settled-700", "settled-50"],
  ["decision Blocked", "critical-solid-foreground", "critical-solid"],
  ["decision Pending/Expired", "slate-600", "slate-100"],

  ["disposition TruePositive", "elevated-700", "card"],
  ["disposition FalsePositive", "slate-500", "card"],
  ["disposition TrueNegative", "slate-500", "card"],
  ["disposition FalseNegative", "critical-700", "critical-50"],

  ["status settled", "settled-700", "settled-50"],
  ["status inflight", "inflight-700", "inflight-50"],
  ["status attention", "attention-700", "attention-50"],
  ["status elevated", "elevated-700", "elevated-50"],
  ["status critical", "critical-700", "critical-50"],
  ["status critical (solid)", "critical-solid-foreground", "critical-solid"],
];

const FLOOR = 4.5;

const GRAPHIC_PAIRS = [
  ["revenue mark new/expansion", "settled-700", "card"],
  ["revenue mark reactivation", "inflight-700", "card"],
  ["revenue mark contraction", "elevated-700", "card"],
  ["revenue mark churn", "critical-700", "card"],
  ["revenue mark opening/closing", "slate-500", "card"],

  ["revenue share bar", "attention-700", "slate-100"],
  ["revenue stage bar settled", "settled-700", "slate-100"],
  ["revenue stage bar inflight", "inflight-700", "slate-100"],
  ["revenue stage bar attention", "attention-700", "slate-100"],
  ["revenue stage bar elevated", "elevated-700", "slate-100"],
  ["revenue stage bar critical", "critical-700", "slate-100"],
];

const GRAPHIC_FLOOR = 3;

for (const [name, foreground, background] of PAIRS) {
  test(`${name} clears AA in both themes`, () => {
    for (const [theme, vars] of [
      ["light", light],
      ["dark", dark],
    ]) {
      const measured = ratio(vars, foreground, background);
      assert.ok(
        measured >= FLOOR,
        `${name} in ${theme}: ${measured.toFixed(2)}:1 against a ${FLOOR}:1 floor`
      );
    }
  });
}

for (const [name, foreground, background] of GRAPHIC_PAIRS) {
  test(`${name} clears the 3:1 non-text floor in both themes`, () => {
    for (const [theme, vars] of [
      ["light", light],
      ["dark", dark],
    ]) {
      const measured = ratio(vars, foreground, background);
      assert.ok(
        measured >= GRAPHIC_FLOOR,
        `${name} in ${theme}: ${measured.toFixed(2)}:1 against a ${GRAPHIC_FLOOR}:1 floor`
      );
    }
  });
}

test("the -500 steps are not quietly reintroduced as chart fills", () => {
  const measured = ratio(light, "settled-500", "card");

  assert.ok(
    measured < GRAPHIC_FLOOR,
    "settled-500 now clears the non-text floor on white; the note in " +
      "components/revenue/Swatch is out of date and the board could use it again"
  );
});

test("the solid critical chip inverts between themes rather than staying put", () => {
  const lightFill = luminance(hslToRgb(resolve(light, light["critical-solid"])));
  const darkFill = luminance(hslToRgb(resolve(dark, dark["critical-solid"])));

  assert.ok(
    darkFill > lightFill,
    "the solid critical fill should be lighter in dark mode, not darker"
  );
});

test("every palette -700 step clears AA on its own -50 tint", () => {
  for (const hue of ["settled", "inflight", "attention", "elevated", "critical"]) {
    for (const [theme, vars] of [
      ["light", light],
      ["dark", dark],
    ]) {
      const measured = ratio(vars, `${hue}-700`, `${hue}-50`);
      assert.ok(
        measured >= FLOOR,
        `${hue}-700 on ${hue}-50 in ${theme}: ${measured.toFixed(2)}:1`
      );
    }
  }
});
