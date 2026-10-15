import assert from "node:assert/strict";
import test from "node:test";

import {
  DOCUMENT_LOCALE,
  currentReaderPreferences,
  formatBytes,
  formatCount,
  formatDecimal,
  formatInstant,
  formatPercent,
  formatRelative,
  formatWallClock,
  publishReaderPreferences,
  utcLabel,
} from "../app/lib/format.ts";
import {
  FALLBACK_LOCALE,
  FALLBACK_TIME_ZONE,
  preferencesPatch,
} from "../app/lib/displayPreferences.ts";
import { formatMoney } from "../app/lib/currency.ts";
import {
  ENGLISH,
  SUPPORTED_LANGUAGES,
  interpolate,
  isSupportedLanguage,
  translate,
} from "../app/lib/i18n.ts";

// 22:30 UTC — deliberately late enough that the three zones below disagree
// about which *day* it is, not merely which hour.
const LATE_EVENING_UTC = "2026-03-14T22:30:00Z";

test("an instant is converted into the reader's zone, not the runtime's", () => {
  const inLondon = formatInstant(LATE_EVENING_UTC, "en-GB", "Europe/London");
  const inSingapore = formatInstant(LATE_EVENING_UTC, "en-GB", "Asia/Singapore");

  assert.match(inLondon, /14 Mar 2026/);
  assert.match(inSingapore, /15 Mar 2026/);
  assert.notEqual(inLondon, inSingapore);
});

test("a deadline names its zone; an ordinary timestamp does not", () => {
  const deadline = formatInstant(
    LATE_EVENING_UTC,
    "en-GB",
    "Asia/Singapore",
    "deadline"
  );
  const ordinary = formatInstant(LATE_EVENING_UTC, "en-GB", "Asia/Singapore");

  assert.match(deadline, /Singapore/);
  assert.ok(!/Singapore/.test(ordinary));
});

test("the locale changes the conventions without changing the instant", () => {
  const british = formatInstant(LATE_EVENING_UTC, "en-GB", "Europe/London");
  const german = formatInstant(LATE_EVENING_UTC, "de-DE", "Europe/London");

  assert.notEqual(british, german);
  assert.match(british, /Mar/);
  assert.match(german, /März|Mär/);
});

test("UTC rides along underneath every timestamp", () => {
  assert.equal(utcLabel(LATE_EVENING_UTC), "2026-03-14 22:30:00Z · UTC");
});

test("an unparseable timestamp is shown as it arrived", () => {
  assert.equal(formatInstant("not-a-date", "en-GB", "Europe/London"), "not-a-date");
  assert.equal(utcLabel("not-a-date"), "");
});

test("an unknown locale or zone degrades instead of throwing", () => {
  assert.doesNotThrow(() =>
    formatInstant(LATE_EVENING_UTC, "not-a-locale", "Europe/London")
  );
  assert.doesNotThrow(() =>
    formatInstant(LATE_EVENING_UTC, "en-GB", "Mars/Olympus_Mons")
  );
});

test("wall-clock formatting does not move the day", () => {
  const chosen = new Date(2026, 2, 14, 0, 0, 0);

  assert.match(formatWallClock(chosen, "en-GB"), /14 Mar 2026/);
  assert.match(formatWallClock(chosen, "de-DE"), /14/);
});

const NOW = new Date("2026-03-14T12:00:00Z");

test("relative time picks the largest unit that fits, in the reader's language", () => {
  const threeDaysAgo = new Date("2026-03-11T12:00:00Z");

  assert.equal(formatRelative(threeDaysAgo, "en-GB", NOW), "3 days ago");
  assert.notEqual(
    formatRelative(threeDaysAgo, "fr-FR", NOW),
    formatRelative(threeDaysAgo, "en-GB", NOW)
  );
});

test("relative time truncates rather than rounds", () => {
  const fiftyNineMinutesAgo = new Date("2026-03-14T11:01:00Z");

  assert.equal(
    formatRelative(fiftyNineMinutesAgo, "en-GB", NOW),
    "59 minutes ago"
  );
});

test("relative time is anchored so a table dates every row against one instant", () => {
  const value = new Date("2026-03-14T11:00:00Z");

  assert.equal(
    formatRelative(value, "en-GB", NOW),
    formatRelative(value, "en-GB", NOW)
  );
});

test("a count is grouped the reader's way", () => {
  assert.equal(formatCount(1204000, "en-GB"), "1,204,000");
  assert.equal(formatCount(1204000, "de-DE"), "1.204.000");
});

test("a percentage takes the fraction, not the multiplied number", () => {
  assert.equal(formatPercent(0.734, "en-GB"), "73.4%");
});

test("a decimal does not grow a .00 it did not earn", () => {
  assert.equal(formatDecimal(12, "en-GB"), "12");
  assert.equal(formatDecimal(12.345, "en-GB"), "12.35");
});

test("bytes group the number and leave the unit in English", () => {
  assert.equal(formatBytes(2_097_152, "en-GB"), "2 MB");
  assert.match(formatBytes(1536, "de-DE"), /1,5 KB/);
});

test("money follows the document's currency and not the reader", () => {
  const invoiced = formatMoney(1200, "EUR");

  assert.equal(invoiced, formatMoney(1200, "EUR"));
  assert.match(invoiced, /€/);
  assert.match(invoiced, /1,200\.00/);
});

test("the document locale is pinned to what the server writes into the PDF", () => {
  assert.equal(DOCUMENT_LOCALE, "en-GB");
  assert.equal(DOCUMENT_LOCALE, FALLBACK_LOCALE);
});

test("the reader's pair starts at the API's own fallback", () => {
  const initial = currentReaderPreferences();
  assert.equal(initial.locale, FALLBACK_LOCALE);
  assert.equal(initial.timeZone, FALLBACK_TIME_ZONE);
});

test("the provider's pair reaches code outside the React tree", () => {
  publishReaderPreferences("fr-FR", "Asia/Singapore");

  const published = currentReaderPreferences();
  assert.equal(published.locale, "fr-FR");
  assert.equal(published.timeZone, "Asia/Singapore");

  publishReaderPreferences(FALLBACK_LOCALE, FALLBACK_TIME_ZONE);
});

test("an untouched preference is omitted, and a cleared one is an explicit null", () => {
  const current = { locale: "fr-FR", timeZone: "Europe/Paris" };

  assert.deepEqual(preferencesPatch(current, current), {});

  const zoneOnly = preferencesPatch(
    { locale: "fr-FR", timeZone: "Asia/Singapore" },
    current
  );
  assert.deepEqual(zoneOnly, { timeZone: "Asia/Singapore" });
  assert.ok(!("locale" in zoneOnly));

  const cleared = preferencesPatch({ locale: null, timeZone: null }, current);
  assert.deepEqual(cleared, { locale: null, timeZone: null });
  assert.equal(JSON.stringify(cleared), '{"locale":null,"timeZone":null}');
});

test("a missing key falls through to English, one key at a time", () => {
  const french = { "cases.list.title": "Dossiers" };

  assert.equal(translate(french, "cases.list.title"), "Dossiers");
  assert.equal(translate(french, "nav.dashboard"), ENGLISH["nav.dashboard"]);
});

test("a key with no translation anywhere resolves to itself", () => {
  assert.equal(translate({}, "some.key.nobody.wrote"), "some.key.nobody.wrote");
});

test("placeholders are positional, matching the API's string.Format", () => {
  assert.equal(
    translate({ "x": "{0} of {1} cases" }, "x", [3, 12]),
    "3 of 12 cases"
  );
  assert.equal(interpolate("{1} then {0}", ["b", "a"]), "a then b");
});

test("a placeholder with no argument stays visible", () => {
  assert.equal(interpolate("waiting on {0}", []), "waiting on {0}");
});

test("the supported languages are exactly the API's Language enum", () => {
  assert.deepEqual([...SUPPORTED_LANGUAGES], ["en", "fr", "de", "es", "nl"]);
  assert.ok(isSupportedLanguage("nl"));
  assert.ok(!isSupportedLanguage("pt"));
});

test("every English message is a string with balanced placeholders", () => {
  for (const [key, message] of Object.entries(ENGLISH)) {
    assert.equal(typeof message, "string", `${key} is not a string`);
    assert.ok(message.length > 0, `${key} is empty`);
    const opens = (message.match(/\{/g) ?? []).length;
    const placeholders = (message.match(/\{\d+\}/g) ?? []).length;
    assert.equal(opens, placeholders, `${key} has a malformed placeholder`);
  }
});
