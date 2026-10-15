# Accessibility Conformance Report

**Product:** Transaction Monitoring Console (web)
**Standard:** WCAG 2.2 Level AA
**Report date:** 9 September 2026
**Report version:** 1.0 — first issue
**Evaluation method:** manual code review of the console source, arithmetic
verification of the colour palette (`tests/contrast.test.mjs`), and static
analysis of the JSX for named controls, keyboard-reachable targets and heading
structure. **No assistive-technology testing has been carried out yet** — see
[What has not been evaluated](#what-has-not-been-evaluated).

---

## How to read this document

This is a VPAT-style report, written in the WCAG 2.2 conformance vocabulary that
regulated and public-sector buyers ask for by name. It is written to be
**useful rather than reassuring**: where the console does not conform, this says
so, says what the failure looks like to somebody using the product, and says
what would fix it.

The conformance levels used are the standard ones:

| Term | Meaning |
| --- | --- |
| **Supports** | The functionality meets the criterion without known defects. |
| **Partially supports** | Some functionality meets it; specific exceptions are named. |
| **Does not support** | The majority of functionality does not meet it. |
| **Not applicable** | The criterion is not relevant to this product. |
| **Not evaluated** | Assessing it requires testing that has not yet been done. |

One caveat that colours the whole document: this report is the product of a
**first** accessibility pass over a console of roughly 185 components that was
built without a stated accessibility target. It is an honest account of a
baseline, not the output of a completed audit. The claims marked *Supports* have
been verified by reading the code that produces them; they have not been
verified by a person using a screen reader, and those are different kinds of
evidence.

---

## Scope

**In scope.** The signed-in console at `app/(console)` — case queues and case
detail, alerts, transactions, parties, rules and rule sets, queues, approvals,
filings, organisation settings, platform administration — together with the
shared shell (sidebar, header, dialogs, tables, toasts) that every one of those
screens is rendered inside.

**Also in scope.** The public status page at `app/status`, and the sign-in,
registration and landing screens. These are reachable without an account and are
the pages a customer reaches during an outage.

**Out of scope.** The API's PDF documents (invoices, credit notes, evidence
packs). These are generated server-side and are assessed against PDF/UA rather
than WCAG; they are covered by the platform's own report.

---

## Summary of conformance

| Principle | Position |
| --- | --- |
| 1. Perceivable | Partially supports |
| 2. Operable | Partially supports |
| 3. Understandable | Partially supports |
| 4. Robust | Partially supports |

**The console does not currently claim full WCAG 2.2 AA conformance.** The
substantive gaps are listed under [Known non-conformances](#known-non-conformances)
and are not hidden in the table below.

---

## Detailed criteria

### Principle 1 — Perceivable

| Criterion | Level | Conformance | Notes |
| --- | --- | --- | --- |
| 1.1.1 Non-text Content | A | **Partially supports** | Every icon-only button in the console now carries an accessible name, and decorative icons inside labelled controls are `aria-hidden` so they are not announced twice. Count badges in the sidebar render the truncated digits (`99+`) to sighted users and a full sentence — "3 requests are waiting for review" — to assistive technology. **Exception:** the Chart.js charts on the dashboard (`app/(console)/page.tsx`) render into a bare `<canvas>` with no text alternative. See [KN-1](#kn-1). |
| 1.2.x Time-based Media | A/AA | **Not applicable** | The console contains no audio or video. |
| 1.3.1 Info and Relationships | A | **Partially supports** | Landmarks are correct on every console screen: one `header`, one `nav` (named "Primary"), one `aside` (named "Console navigation"), one `main`. Tables use `<th>` with `scope="col"` — defaulted in the shared `TableHead` so all ~90 column headings in the console are covered rather than the ones somebody remembered. Headings run one `<h1>` per screen with no skipped levels; verified by static analysis across all `.tsx` files. **Exception:** tab panels are not associated with their tabs. See [KN-2](#kn-2). |
| 1.3.2 Meaningful Sequence | A | **Supports** | Reading order follows DOM order throughout; no CSS reordering is used that changes meaning. |
| 1.3.3 Sensory Characteristics | A | **Supports** | No instruction in the console depends on shape, size or position alone. |
| 1.3.4 Orientation | AA | **Supports** | No orientation lock. |
| 1.3.5 Identify Input Purpose | AA | **Partially supports** | `autocomplete` is set on the sign-in, registration and password-change fields (`username`, `email`, `current-password`, `new-password`). It is not set elsewhere — including the email field on the profile form — though most remaining fields collect data with no defined `autocomplete` token. |
| 1.4.1 Use of Colour | A | **Supports** | Severity, status, decision and disposition are all carried by a **text label** as well as a colour, in every badge component. Disposition additionally carries a distinct icon per value. Two verdict pickers inside case triage — `CloseCaseDialog` and `CaseStatusDialog` — previously signalled the chosen option by border and fill alone and now carry `aria-pressed`. The active tab in `TabBar` carries `aria-selected`; the active nav item carries `aria-current="page"`. |
| 1.4.3 Contrast (Minimum) | AA | **Supports** | All 24 foreground/background pairs used by `SeverityBadge`, `StatusBadge`, `DecisionBadge` and `DispositionBadge` clear 4.5:1 in **both** themes. This is asserted arithmetically on every commit by `tests/contrast.test.mjs`, which parses the tokens out of `globals.css` rather than trusting a comment. Six pairs failed when first measured and were fixed; see [What changed](#what-changed-in-this-pass). |
| 1.4.4 Resize Text | AA | **Partially supports** | Layout is relative and reflows, but the console's dense mono type runs to 10–11px at several places (badges, table eyebrows, sidebar counts). These remain legible at 200% zoom; they have not been tested at 200% *text-only* resize. |
| 1.4.5 Images of Text | AA | **Supports** | No images of text. |
| 1.4.10 Reflow | AA | **Partially supports** | Most screens reflow; the wide data tables (cases, transactions, alerts) scroll horizontally inside their own container rather than reflowing, which the criterion permits for data tables. `SectionNav` collapses to a select below `lg`. Not verified at 320px. |
| 1.4.11 Non-text Contrast | AA | **Not evaluated** | Component boundaries, focus indicators and the icon-only controls have not been measured against the 3:1 floor. The palette work covered text pairs only. See [KN-3](#kn-3). |
| 1.4.12 Text Spacing | AA | **Not evaluated** | No override testing has been done. |
| 1.4.13 Content on Hover or Focus | AA | **Partially supports** | Radix tooltips and popovers are dismissible and hoverable. **Exception:** the console makes heavy use of the native `title` attribute for supplementary explanation, and native tooltips are neither dismissible nor hoverable. In every case the `title` is supplementary to visible content rather than the only source of the information. |

### Principle 2 — Operable

| Criterion | Level | Conformance | Notes |
| --- | --- | --- | --- |
| 2.1.1 Keyboard | A | **Supports** | Static analysis found **no** mouse-only click targets in the console: every `onClick` is on a real `<button>`, `<a>`, or an element carrying the correct role and keyboard handling. The one exception is the option list in `InheritedSelect`, which is an ARIA listbox operated by arrow keys and `aria-activedescendant` — the pattern's intended shape. The full case-triage loop (assign → note → decide → resolve → reopen) is reachable and operable from the keyboard. |
| 2.1.2 No Keyboard Trap | A | **Supports** | **This was a genuine failure and is fixed.** `@uiw/react-codemirror` defaults `indentWithTab` to true, so Tab indented rather than moving focus, and a keyboard user who entered the JSON editor in `RuleBuilder` or on the Evaluate screen could not leave it. Escape now moves focus to the editor's wrapper, which sits in ordinary document order so the next Tab continues past it, and the instruction is rendered visibly beside the editor and referenced by `aria-describedby`. Fixed once in `JsonEditor`, which is the only CodeMirror in the app. |
| 2.1.4 Character Key Shortcuts | A | **Not applicable** | The console defines no single-character shortcuts. |
| 2.2.1 Timing Adjustable | A | **Partially supports** | No session timeout is enforced client-side. Toasts auto-dismiss on sonner's default timing and cannot be extended by the user; each is supplementary to state that remains on screen. |
| 2.2.2 Pause, Stop, Hide | A | **Supports** | The console honours `prefers-reduced-motion`: the evaluation console reads the media query directly, and the one keyframe animation — the indeterminate upload progress bar, which runs for the length of a virus scan that cannot report progress — now stops animating under `motion-reduce` while still showing that the scan is running. |
| 2.4.1 Bypass Blocks | A | **Supports** | A skip link is the first focusable element on every console screen and moves focus to `main`, which is `tabIndex={-1}` so focus genuinely lands there rather than only scrolling. This matters more here than on most products: the sidebar is around thirty links deep with its groups open, and it is the same thirty on every screen. |
| 2.4.2 Page Titled | A | **Partially supports** | The public status page carries its own descriptive title. Every **console** route shares the static "Transaction Monitoring" and does not change per screen. See [KN-4](#kn-4). |
| 2.4.3 Focus Order | A | **Supports** | Focus order follows DOM order. Radix dialogs trap focus while open and return it to the trigger on close. |
| 2.4.4 Link Purpose (In Context) | A | **Supports** | Repeated row actions ("View", "Resolve", assign, unassign, delete) carry the record's own name in their accessible name, so twenty rows do not present twenty identically-named controls. |
| 2.4.5 Multiple Ways | AA | **Supports** | Every record is reachable through both the navigation hierarchy and the per-screen search and filter controls. |
| 2.4.6 Headings and Labels | AA | **Supports** | Headings describe their sections; every form control in the extracted screens has a programmatically associated label. |
| 2.4.7 Focus Visible | AA | **Supports** | Every interactive primitive (`button`, `input`, `textarea`, `select`, dialog close, nav item, tab, combobox) carries `focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2`. Verified by grep: no `focus:outline-none` in the codebase is left without a paired ring. |
| 2.4.11 Focus Not Obscured (Minimum) | AA | **Not evaluated** | The sticky `SectionNav` column and the fixed header could obscure a focused element on a short viewport. Not tested. |
| 2.5.1 Pointer Gestures | A | **Supports** | No path-based or multipoint gestures. |
| 2.5.2 Pointer Cancellation | A | **Supports** | Activation is on `click`/`pointerup` throughout. The one `onPointerDown` — selecting an option in `InheritedSelect` — is there to beat the input's blur and is within the pattern. |
| 2.5.3 Label in Name | A | **Partially supports** | Where a control's accessible name adds the record's name to the visible label, the visible text is retained as a prefix ("Resolve — ACME-1043"), which satisfies the criterion. Not systematically audited across the unextracted screens. |
| 2.5.4 Motion Actuation | A | **Not applicable** | No motion actuation. |
| 2.5.7 Dragging Movements | AA | **Not applicable** | No dragging in the console. |
| 2.5.8 Target Size (Minimum) | AA | **Supports** | Standard buttons are 32–40px and the icon-only row actions are 32×32px, both clearing the 24×24 minimum. The one target below it — the 20×20px filter-clear chip on the alerts screen — was raised to 24×24 in this pass. |

### Principle 3 — Understandable

| Criterion | Level | Conformance | Notes |
| --- | --- | --- | --- |
| 3.1.1 Language of Page | A | **Supports** | The root layout renders `<html lang="en">`, which is the honest default for a server render that has no session yet, and `LocalizationProvider` corrects `document.documentElement.lang` to the reader's resolved language once `/users/me/preferences` has answered. Without this, shipping five languages against a hard-coded `en` would have had a French reader's words pronounced by an English speech synthesiser — a defect the localisation work would otherwise have *created*. |
| 3.1.2 Language of Parts | AA | **Partially supports** | User-authored text (case names, notes, reason codes, discount descriptions) is rendered in whatever language it was written in and is not marked with `lang`. The console deliberately does not translate this text; it also does not know its language. |
| 3.2.1 On Focus | A | **Supports** | No context change on focus. |
| 3.2.2 On Input | A | **Supports** | No form submits on input. Filters update the listing in place, which is the expected behaviour of a filter rather than a change of context. |
| 3.2.3 Consistent Navigation | AA | **Supports** | The sidebar is identical in order and content on every screen, varying only by permission. |
| 3.2.4 Consistent Identification | AA | **Supports** | Shared components mean the same action is presented the same way throughout. |
| 3.2.6 Consistent Help | A | **Not applicable** | The console offers no help mechanism. |
| 3.3.1 Error Identification | A | **Partially supports** | Validation errors from the API are surfaced as toasts, and several panels additionally place the message inline beside the field it belongs to (hold policy, language and region). Most panels rely on the toast alone, which is not programmatically associated with the field. See [KN-5](#kn-5). |
| 3.3.2 Labels or Instructions | AA | **Supports** | Labels are present on the extracted screens; the JSON editor carries its escape instruction visibly. |
| 3.3.3 Error Suggestion | AA | **Partially supports** | The API's messages name the field and the constraint. Where the console adds its own refusals, the wording explains the cause rather than restating the rule. |
| 3.3.4 Error Prevention (Legal, Financial, Data) | AA | **Supports** | Destructive and irreversible actions are confirmed: sandbox reset requires typing a confirmation word, closing a case opens a form rather than acting on a press, and turning off organisation-wide hold expiry is confirmed separately. |
| 3.3.7 Redundant Entry | A | **Supports** | No step asks for information already given. |
| 3.3.8 Accessible Authentication (Minimum) | AA | **Supports** | Sign-in is username and password; password managers are not obstructed, and `autocomplete` is set to let them work. No cognitive function test. |

### Principle 4 — Robust

| Criterion | Level | Conformance | Notes |
| --- | --- | --- | --- |
| 4.1.2 Name, Role, Value | A | **Partially supports** | Icon-only buttons are named. Disclosure toggles carry `aria-expanded` and `aria-controls`. Tabs carry `role="tab"`, `aria-selected` and a roving tabindex with arrow-key navigation. The locale and timezone pickers are full ARIA comboboxes. Menu triggers carry `aria-haspopup`. **Exception:** the verdict pickers use `aria-pressed` where `radiogroup`/`radio` would model their exclusivity more precisely. See [KN-6](#kn-6). |
| 4.1.3 Status Messages | AA | **Partially supports** | The toast region is a named live region. The preference readout, the format preview and the pagination position are all `aria-live="polite"`. **Exception:** sonner offers no per-toast politeness, so an error toast — a refused decision, a rate limit, an exhausted sandbox credit — is announced politely rather than assertively. See [KN-7](#kn-7). |

---

## Known non-conformances

Each item states what the defect is, who it affects and what would fix it.

### KN-1 — Charts have no text alternative
**Criterion:** 1.1.1 (A) · **Severity:** high
The dashboard (`app/(console)/page.tsx`) renders Chart.js charts into a bare
`<canvas>`. To a screen reader they are empty. The figures behind them are
generally available in a table elsewhere on the same screen, but not always, and
nothing tells the reader that.
**Fix:** give each chart a text summary in a visually hidden element, or render
the underlying series as a `<table>` beside it.
**Partly addressed (revenue board).** The charts on `/admin/revenue` take the
second of those: the drawings are inline SVG marked `aria-hidden`, and every
figure they depict is rendered beside them as an ordinary captioned `<table>`.
This does **not** close KN-1 — the dashboard's Chart.js canvases are unchanged,
and they are what the finding is about.

### KN-2 — Tab panels are not associated with their tabs
**Criterion:** 1.3.1 (A), 4.1.2 (A) · **Severity:** medium
`TabBar` announces itself as a tab list with a correctly selected tab, but the
panel each tab reveals is rendered by the calling screen and carries no
`role="tabpanel"` or `aria-labelledby`. `aria-controls` was deliberately **not**
added pointing at an id that does not exist — a wrong association is worse than a
missing one.
**Fix:** have each caller wrap its panel and pass the id back, or move panel
rendering into `TabBar`.

### KN-3 — Non-text contrast unmeasured
**Criterion:** 1.4.11 (AA) · **Severity:** medium
The contrast work covered text on its background. Component borders, the focus
ring against adjacent surfaces, and the icons inside icon-only buttons have not
been measured against the 3:1 floor.
**Fix:** extend `tests/contrast.test.mjs` with the boundary and focus-ring pairs.
The harness already parses every token out of `globals.css`, so this is a matter
of adding pairs to the list rather than new machinery.
**Partly addressed (revenue board).** The chart marks on `/admin/revenue` are
now measured against the 3:1 floor in both themes, using exactly that approach.
Doing it found a real defect before the screen shipped: the palette's -500 step,
which is the obvious one for a filled area, measures between 1.70:1 and 3.37:1
against the card surface in light mode, so four of the five hues were under the
floor. The board uses the -700 steps instead. Borders, focus rings and
icon-only button glyphs across the rest of the console remain unmeasured, so the
finding stands.

### KN-4 — The document title does not change per console screen
**Criterion:** 2.4.2 (A) · **Severity:** medium
Every console route is titled "Transaction Monitoring". The public status page
carries its own descriptive title, so this is a console-only gap. A screen
reader user with several tabs open, or one relying on the title to confirm that
a navigation landed, gets nothing.
**Fix:** per-route `metadata`, or a title effect keyed on the page heading.

### KN-5 — Field errors are not programmatically associated
**Criterion:** 3.3.1 (A) · **Severity:** medium
Most panels report validation failures as a toast. The toast is announced, but
it is not tied to the field, so a screen reader user moving through the form
does not meet the error at the control that caused it.
**Fix:** the pattern `HoldPolicyPanel` already uses — map the API's error keys to
fields, render inline, and reference with `aria-describedby` plus `aria-invalid`.
Applying it to the remaining panels is mechanical.

### KN-6 — Verdict pickers use `aria-pressed` rather than radio semantics
**Criterion:** 4.1.2 (A) · **Severity:** low
`CloseCaseDialog` and `CaseStatusDialog` present mutually exclusive choices as
toggle buttons. The state is now announced, but the exclusivity is not modelled.
`radiogroup` was not adopted in this pass because it makes the set a single tab
stop with arrow-key navigation, which changes how analysts already work the
dialog — a change worth making deliberately rather than as a side effect of an
accessibility sweep.
**Fix:** adopt `radiogroup`/`radio` with a roving tabindex, as `TabBar` now does.

### KN-7 — Error toasts are announced politely
**Criterion:** 4.1.3 (AA) · **Severity:** medium
sonner renders one live region at `polite` for all toasts. A refused decision, a
rate limit or an exhausted sandbox credit therefore waits for the screen reader
to finish whatever it was saying — and on this console a toast is often the only
account of why a button appeared to do nothing.
**Fix:** an `aria-live="assertive"` region of the console's own for failures, or
replace sonner's region.

### KN-8 — Date pickers select in the browser's zone
**Criterion:** none — correctness rather than conformance · **Severity:** low
`DateRangePicker` and `DateTimePicker` render their labels in the reader's
locale, but the calendar still picks a day in the **browser's** zone rather than
the reader's stated one. Somebody in `Asia/Singapore` on a machine set to
`Europe/London` can pick a day whose boundaries are not the ones they mean.
Recorded here because it is the one place the display preference does not yet
reach. @see `formatWallClock` in `app/lib/format.ts` for why the labels
deliberately do *not* convert.

---

## What has not been evaluated

Stated plainly, because the absence of a finding is not a finding:

- **No assistive technology testing.** Nothing in this report has been confirmed
  with NVDA, JAWS, VoiceOver or TalkBack. Every "Supports" is a claim about the
  code, not about the experience.
- **No automated audit tool** (axe, Lighthouse, Pa11y) has been run.
- **No testing with disabled users.**
- **No mobile or touch testing.**
- **No testing at 320px, 200% text-only zoom, or with a text-spacing override.**
- **Windows High Contrast Mode** has not been checked; the console paints its own
  surfaces throughout and is likely to need work there.

The next step that would most change this document is an axe pass across the
console followed by a screen-reader walkthrough of the case triage loop, since
that is the workflow an analyst spends the day in.

---

## What changed in this pass

Recorded so a reader can tell what is newly true from what was always true.

**Failures found and fixed**

1. **A keyboard trap in the JSON editor** — WCAG 2.1.2, Level A, and one of the
   few criteria that fails a page outright rather than degrading it.
   `@uiw/react-codemirror` defaults `indentWithTab` to true, so Tab indented
   instead of moving focus: a keyboard user who entered the editor in
   `RuleBuilder` or on the Evaluate screen could not leave it, and nothing on
   screen said so. Escape now releases focus to the editor's wrapper, and the
   instruction is rendered visibly and referenced by `aria-describedby`. Fixed
   once in `JsonEditor`, which is the only CodeMirror in the app.

2. **Six badge colour pairs below 4.5:1** — 1.4.3. Worst was severity `High` in
   list rows at **3.30:1**. The palette guarantees each `-700` step against its
   own `-50` tint; `SeverityBadge` had reached for `-100` and `-200`, which were
   never covered by that promise — so the ramp looked like it climbed and quietly
   stopped being readable as it did. The two worst pairs were High and Critical:
   exactly the rows an analyst scans a queue for. Fills moved to `-50` and the
   ramp's climb moved onto border weight, which costs no contrast. Now asserted
   arithmetically on every commit by `tests/contrast.test.mjs`.

3. **An unnamed destructive button** — 4.1.2. The case-note delete had an icon,
   no text and no title, and announced as "button". Named, along with 29 other
   icon controls whose only name was a `title` tooltip — which is a valid name
   but is not surfaced on keyboard focus or touch.

4. **Tabs with no state** — 1.4.1 and 4.1.2. `TabBar` marked the active tab with
   a colour and an underline only. It is now a real tab list with `aria-selected`,
   a roving tabindex and arrow-key navigation, across roughly fifteen screens.

5. **Verdict pickers with no state**, inside the case triage loop — 1.4.1.

6. **No skip link** — 2.4.1, past a thirty-item sidebar repeated on every screen.

7. **Table headings without `scope`** — 1.3.1. Fixed by defaulting it in the
   shared `TableHead`, so all ~90 column headings are covered rather than the
   ones somebody remembered.

8. **A dialog close button that submitted forms** — its `type` was unset, so
   dismissing a dialog that wrapped a form saved it.

9. **A pointer target below the minimum** — 2.5.8. The alerts filter-clear chip
   was 20×20px against a 24×24 floor.

10. **The one keyframe animation ignored `prefers-reduced-motion`** — 2.2.2.

**A defect this change would otherwise have created**

11. **`<html lang>` following the reader** — 3.1.1. Shipping a console that can
    render in a reader's own language against a hard-coded `lang="en"` would have
    had a French reader's words pronounced by an English speech synthesiser,
    which is close to unintelligible and strictly worse than the English console
    they had before.
    `LocalizationProvider` now sets `document.documentElement.lang` from the
    resolved language once the preferences endpoint answers. Worth naming
    separately: it is the localisation half of this work creating an
    accessibility regression, and it was caught only because the two were done
    together.

## Localisation and accessibility, together

The two halves of this change are related, and the relationship is worth stating
because it is the reason the timezone work belongs in an accessibility report at
all.

A **deadline read in the wrong time zone is a missed statutory filing**. Before
this change every timestamp in the console was formatted by `toLocaleString()`
with no arguments, which resolves against the *machine* — so a London analyst
covering the Singapore desk read every deadline in Europe/London, and the screen
looked correct all day. That is not an accessibility criterion, but it is the
same class of defect the criteria exist to catch: information the interface
presents confidently and wrongly, in a way the person reading it has no way to
notice.

The console now takes locale and time zone from `GET /users/me/preferences`,
which resolves them through user → organisation → installation, and
`components/LocalTime` is the only way a timestamp reaches the DOM. Timestamps
that are **deadlines** name their zone on their face rather than in a tooltip.

Two things deliberately do **not** follow the reader:

- **Money**, which follows the invoice's own currency and a pinned locale,
  because a document is read by whoever the customer hands it to and every copy
  has to say the same thing. The API's `CurrencyFormatter` makes the same call.
- **Wire formats** — `yyyy-MM-dd` in a query string — which must be identical
  for every reader.

And user-authored text is never translated, in either direction: a case name, an
analyst's note, an incident update or a reason code is rendered exactly as it
was written. A translation of somebody else's words is a claim about what they
meant, and on a screen where those words are evidence, it is not a claim this
console has standing to make.

### How far the translation itself has got

The **machinery** and the **extraction** are both complete. Every string the
product authors — 1,549 keys across 68 namespaces, in 195 files — is rendered
through `useTranslation()`; none is left inline in the JSX. A test
(`tests/catalogue.test.mjs`) walks the source on every commit and fails on a
user-facing literal that is not a translation call, so the extraction cannot
quietly come undone as new screens are written.

Three things are deliberately exempt, and the test knows about each:

- **The public status page.** Server-rendered, anonymous, and outside
  `LocalizationProvider` by design — it has to render during an outage with no
  session and so no account to have a language. Translating it needs a
  different mechanism from the console's.
- **Format examples.** `placeholder="RE-01234"`, `placeholder="0.00"`, a nil
  GUID in a scope picker. These show the *shape* a field expects; a French
  reader still types a GUID as a GUID.
- **User-authored text**, which is never translated in either direction.

What is **not** done is the translating. `fr`, `de`, `es` and `nl` are empty
files. That is the intended state rather than an oversight — the fallback is
per key, so a catalogue holding ten strings reads those ten in the reader's
language and everything else in English, and a translator can land one screen
at a time without any of it being wired up again.

The accessibility consequence is worth naming: a reader who selects French
today gets a French `lang` attribute on a page of English words. That is a
worse mismatch than English text with an English `lang`, and it argues for
holding a language back until its catalogue is populated — not for reverting
the attribute.

---

## Feedback

Accessibility problems with this console should be raised through the same
support channel as any other defect, and will be triaged against this document.
Where a report names a criterion listed above as *Partially supports* or *Not
evaluated*, it is confirming a known gap rather than finding a new one, and will
be prioritised accordingly.
