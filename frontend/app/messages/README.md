# Message catalogues

One file per language the product ships translations for, matching the API's
`Language` enum exactly: `en`, `fr`, `de`, `es`, `nl`. The list is closed on
purpose — `GET /localization/catalogue` publishes it, and a console offering a
language the platform has no translations for is a promise the platform will not
keep.

`en.json` is the source of truth and holds every string the product authors —
1,551 keys across 68 namespaces. `fr`, `de`, `es` and `nl` are complete against
it: same key set, same order, same blank-line blocks, so any two files diff
line for line.

The fallback is still per key, and that is what makes the four safe to edit one
screen at a time. A key added to English and not yet translated reads in English
in the other four rather than breaking them — see `loadCatalogue` and the
`catalogue[key] ?? fallback[key]` in `app/lib/i18n.ts`. Nothing needs wiring up
when a translation lands; the file is the whole mechanism.

`glossaries/` holds the term list each language was translated against — the
decisions that have to hold across all 1,551 strings rather than within one of
them. **Read your language's glossary before adding a string to it.** The cost
of a catalogue this size is not translating a string, it is translating the same
English word two different ways in two places, six months apart.

`tests/catalogue.test.mjs` enforces the rest: no user-facing literal outside the
catalogue, no key nobody renders, no malformed placeholder, and no key in
another language that English has since dropped.

## Keys

`domain.subject.qualifier`, dot-separated, camelCase inside a segment:

```
nav.cases.all
cases.detail.note.placeholder
preferences.timeZone.help
```

They mirror `Core/Localization/TranslationKeys.cs` in the API repo, and where
the console names something the platform has already named, **it reuses the
platform's key**. `severity.critical` is one string with one translation, so a
badge in this console and an email from the platform cannot disagree about what
Critical is called in French.

## Placeholders

Positional — `{0}`, `{1}` — because the API formats with `string.Format` and
somebody translating both catalogues in one sitting should not have to hold two
syntaxes in their head.

```json
"nav.badge.approvalsWaiting": "{0} requests are waiting for review"
```

## What never goes in here

Anything a user wrote. Case names, case notes, discount descriptions, incident
notes, reason code labels, party names, rule names. The API does not translate
them either: a translation of somebody else's words is a claim about what they
meant, and on a screen where those words are evidence, that is a claim this
console has no standing to make.

## Adding a string

1. Add the key to `en.json`, in the block its domain belongs to.
2. Use it through `useTranslation()`. Never `t("some.key") ?? "Fallback"` — the
   fallback chain already ends at English, and a second one hides a missing key.
3. Translate it into the other four, against `glossaries/<lang>.md`, and add any
   new term you had to decide to that glossary. Leaving them is not a failure —
   the key falls back to English and the console stays legible — but the four
   are complete now, and a gap left today is a gap somebody has to rediscover
   from a screenshot later.

Before translating a terse string, **read the component that renders it.** The
catalogue strips the context that disambiguates, and the strings most likely to
be read wrongly are the short ones. Two that were:

- `escalations.uniquePerOrganisationComparedWithout` — "compared without case"
  is *letter* case. In a case-management console the other reading is right
  there, and it inverts the sentence.
- `alerts.settle` — a button on a held transaction that opens a Release/Block
  form. It is *deciding* the hold, not settling a payment. Three of the four
  languages first rendered it as financial settlement.
