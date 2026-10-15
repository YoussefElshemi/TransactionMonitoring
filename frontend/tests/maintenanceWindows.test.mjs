import assert from "node:assert/strict";
import test from "node:test";

import {
  MINIMUM_NOTICE_HOURS,
  canCancel,
  canEdit,
  editRefusal,
  formatNotice,
  isProperlyAnnounced,
  isRunning,
  maintenanceRefusal,
  noticeHours,
  noticeSentence,
  plannedNotice,
  soonestFirst,
  windowLength,
} from "../app/lib/maintenanceWindows.ts";

const NOW = new Date("2026-06-15T12:00:00Z");

const window = (overrides = {}) => ({
  maintenanceWindowId: "mw",
  title: "Database upgrade",
  description: "Replacing the primary.",
  status: "Scheduled",
  components: ["Api"],
  scheduledStart: "2026-06-20T02:00:00Z",
  scheduledEnd: "2026-06-20T04:00:00Z",
  announcedAt: "2026-06-15T12:00:00Z",
  createdAt: "2026-06-15T12:00:00Z",
  updatedAt: "2026-06-15T12:00:00Z",
  ...overrides,
});

test("notice is the gap between the announcement and the start", () => {
  // Announced 15th noon, starts 20th 02:00 — 110 hours.
  assert.equal(noticeHours(window()), 110);
});

test("a window announced with enough notice is excluded from availability", () => {
  assert.equal(isProperlyAnnounced(window()), true);
  assert.match(noticeSentence(window()), /excluded from availability targets/);
});

test("a short-notice window says what it costs, not just how short it is", () => {
  const late = window({
    announcedAt: "2026-06-20T00:00:00Z",
    scheduledStart: "2026-06-20T02:00:00Z",
  });

  assert.equal(isProperlyAnnounced(late), false);
  const sentence = noticeSentence(late);

  assert.match(sentence, /2 hours/);
  assert.match(sentence, new RegExp(`${MINIMUM_NOTICE_HOURS}-hour minimum`));
  assert.match(sentence, /count against every customer's availability target/);
});

test("a missing announcement is unknown, not zero notice", () => {
  const unannounced = window({ announcedAt: null });

  assert.equal(noticeHours(unannounced), null);
  assert.equal(isProperlyAnnounced(unannounced), null);
  assert.match(noticeSentence(unannounced), /no way to tell/);
});

test("notice is phrased in whichever unit reads naturally", () => {
  assert.equal(formatNotice(0.5), "30 minutes");
  assert.equal(formatNotice(1), "1 hour");
  assert.equal(formatNotice(26), "26 hours");
  assert.equal(formatNotice(168), "7 days");
});

test("nothing to judge yet returns nothing, rather than a reassuring default", () => {
  assert.equal(plannedNotice("", NOW), null);
  assert.equal(plannedNotice("not a date", NOW), null);
});

test("the form says a long-notice window will be excluded", () => {
  const verdict = plannedNotice("2026-06-30T02:00:00Z", NOW);

  assert.equal(verdict.clears, true);
  assert.match(verdict.sentence, /excluded from availability targets/);
});

test("the form warns before the save, not after", () => {
  const verdict = plannedNotice("2026-06-15T20:00:00Z", NOW);

  assert.equal(verdict.clears, false);
  assert.match(verdict.sentence, /8 hours/);
  assert.match(verdict.sentence, /as though the service had simply failed/);
});

test("a start in the past is no notice at all", () => {
  const verdict = plannedNotice("2026-06-01T00:00:00Z", NOW);

  assert.equal(verdict.clears, false);
  assert.match(verdict.sentence, /in the past/);
});

test("the verdict is a flag rather than a phrase to be matched", () => {
  assert.equal(typeof plannedNotice("2026-06-30T02:00:00Z", NOW).clears, "boolean");
});

test("only a scheduled window may be moved or cancelled", () => {
  assert.equal(canEdit(window({ status: "Scheduled" })), true);
  assert.equal(canEdit(window({ status: "InProgress" })), false);
  assert.equal(canCancel(window({ status: "Completed" })), false);
});

test("each refusal says why, in terms of what already happened", () => {
  assert.equal(editRefusal(window({ status: "Scheduled" })), null);
  assert.match(
    editRefusal(window({ status: "InProgress" })),
    /customers are already in it/i
  );
  assert.match(
    editRefusal(window({ status: "Cancelled" })),
    /Schedule a new one/
  );
});

const draft = (overrides = {}) => ({
  title: "Database upgrade",
  description: "Replacing the primary.",
  components: ["Api"],
  scheduledStart: "2026-06-20T02:00:00Z",
  scheduledEnd: "2026-06-20T04:00:00Z",
  ...overrides,
});

test("a complete window is accepted", () => {
  assert.equal(maintenanceRefusal(draft()), null);
});

test("a window over no component is refused, with the consequence named", () => {
  assert.match(
    maintenanceRefusal(draft({ components: [] })),
    /tells a customer their integration is fine when it is about to stop/
  );
});

test("a window with no end is an outage", () => {
  assert.match(
    maintenanceRefusal(draft({ scheduledEnd: "" })),
    /A window with no end is an outage/
  );
});

test("a window that ends before it starts is refused", () => {
  assert.match(
    maintenanceRefusal(
      draft({
        scheduledStart: "2026-06-20T04:00:00Z",
        scheduledEnd: "2026-06-20T02:00:00Z",
      })
    ),
    /ends at or before it starts/
  );
});

test("short notice is never a refusal — an emergency window is legitimate", () => {
  assert.equal(
    maintenanceRefusal(draft({ scheduledStart: "2026-06-15T13:00:00Z" })),
    null
  );
});

test("windows are listed soonest first", () => {
  const later = window({ scheduledStart: "2026-07-01T00:00:00Z" });
  const sooner = window({ scheduledStart: "2026-06-16T00:00:00Z" });

  assert.deepEqual(
    soonestFirst([later, sooner]).map((entry) => entry.scheduledStart),
    ["2026-06-16T00:00:00Z", "2026-07-01T00:00:00Z"]
  );
});

test("a window covering now is running", () => {
  const running = window({
    scheduledStart: "2026-06-15T11:00:00Z",
    scheduledEnd: "2026-06-15T13:00:00Z",
  });

  assert.equal(isRunning(running, NOW), true);
  assert.equal(isRunning(window(), NOW), false);
});

test("window length reads in the unit that suits it", () => {
  assert.equal(windowLength(window()), "2 hours");
  assert.equal(
    windowLength(
      window({
        scheduledStart: "2026-06-20T02:00:00Z",
        scheduledEnd: "2026-06-20T02:30:00Z",
      })
    ),
    "30 minutes"
  );
});
