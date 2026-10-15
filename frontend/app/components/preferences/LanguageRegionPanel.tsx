"use client";

import { useEffect, useMemo, useState } from "react";
import { toast } from "sonner";

import { useLocalization, useTranslation } from "@/context/LocalizationContext";
import { InheritedSelect } from "@/components/preferences/InheritedSelect";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardFooter } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { API_BASE, reportApiError } from "@/lib/api";
import {
  LocalizationCatalogue,
  StatedPreferences,
  fetchLocalizationCatalogue,
  languageLabel,
  localeLabel,
  preferencesPatch,
  timeZoneLabel,
} from "@/lib/localization";
import { formatInstant } from "@/lib/format";
import { apiFetch } from "@/lib/http";

export function LanguageRegionPanel({
  onUnauthorized,
}: {
  onUnauthorized: () => void;
}) {
  const t = useTranslation();
  const { locale, timeZone, stated, ready, refresh } = useLocalization();

  const [catalogue, setCatalogue] = useState<LocalizationCatalogue | null>(null);
  const [catalogueFailed, setCatalogueFailed] = useState(false);
  const [draft, setDraft] = useState<StatedPreferences>(stated);
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    let current = true;

    fetchLocalizationCatalogue()
      .then((loaded) => {
        if (current) setCatalogue(loaded);
      })
      .catch(() => {
        if (current) setCatalogueFailed(true);
      });

    return () => {
      current = false;
    };
  }, []);

  useEffect(() => {
    setDraft(stated);
  }, [stated.locale, stated.timeZone]);

  const draftLanguage = draft.locale
    ? draft.locale.split("-")[0].toLowerCase()
    : null;

  const patch = preferencesPatch(draft, stated);
  const dirty = Object.keys(patch).length > 0;

  const chooseLanguage = (language: string | null) => {
    if (language === null) {
      setDraft((current) => ({ ...current, locale: null }));
      return;
    }

    if (draft.locale?.toLowerCase().startsWith(`${language.toLowerCase()}-`)) {
      return;
    }

    const match = catalogue?.locales.find((tag) =>
      tag.toLowerCase().startsWith(`${language.toLowerCase()}-`)
    );

    setDraft((current) => ({ ...current, locale: match ?? language }));
  };

  const save = async () => {
    if (!dirty) return;

    setSaving(true);

    try {
      const response = await apiFetch(`${API_BASE}/users/me/preferences`, {
        method: "PUT",
        headers: { "Content-Type": "application/json" },
        credentials: "include",
        // `JSON.stringify` keeps an explicit null, which is what carries
        // "I no longer state one" as distinct from "leave it alone".
        body: JSON.stringify(patch),
      });

      if (response.ok) {
        toast.success(t("preferences.saved"));
        await refresh();
        return;
      }

      await reportApiError(response, {
        fallback: t("preferences.saveFailed"),
        onUnauthorized,
      });
    } catch {
      toast.error(t("preferences.saveFailed"));
    } finally {
      setSaving(false);
    }
  };

  const preview = useMemo(() => {
    const previewLocale = draft.locale ?? locale;
    const previewZone = draft.timeZone ?? timeZone;

    return formatInstant(new Date(), previewLocale, previewZone, "deadline");
  }, [draft.locale, draft.timeZone, locale, timeZone]);

  if (catalogueFailed) {
    return (
      <Card>
        <CardContent className="pt-6">
          <p className="text-sm text-slate-500">
            {t("preferences.catalogueFailed")}
          </p>
        </CardContent>
      </Card>
    );
  }

  if (!catalogue || !ready) {
    return (
      <Card>
        <CardContent className="space-y-4 pt-6">
          {Array.from({ length: 3 }).map((_, i) => (
            <Skeleton key={i} className="h-16 w-full" />
          ))}
        </CardContent>
      </Card>
    );
  }

  return (
    <Card className="flex flex-col">
      <CardContent className="flex-1 space-y-6 pt-6">
        <p className="text-sm leading-relaxed text-slate-500">
          {t("preferences.description")}
        </p>

        <InheritedSelect
          label={t("preferences.language.label")}
          help={t("preferences.language.help")}
          value={draftLanguage}
          inheritedValue={locale.split("-")[0].toLowerCase()}
          inheritedFrom={t("preferences.inherited")}
          options={catalogue.languages}
          optionLabel={languageLabel}
          onChange={chooseLanguage}
          disabled={saving}
        />

        <InheritedSelect
          label={t("preferences.locale.label")}
          help={t("preferences.locale.help")}
          value={draft.locale}
          inheritedValue={locale}
          inheritedFrom={t("preferences.inherited")}
          options={catalogue.locales}
          optionLabel={(tag) => localeLabel(tag, locale)}
          onChange={(next) =>
            setDraft((current) => ({ ...current, locale: next }))
          }
          disabled={saving}
        />

        <InheritedSelect
          label={t("preferences.timeZone.label")}
          help={t("preferences.timeZone.help")}
          value={draft.timeZone}
          inheritedValue={timeZone}
          inheritedFrom={t("preferences.inherited")}
          options={catalogue.timeZones}
          optionLabel={(zone) => timeZoneLabel(zone, locale)}
          onChange={(next) =>
            setDraft((current) => ({ ...current, timeZone: next }))
          }
          disabled={saving}
        />

        <p
          aria-live="polite"
          className="rounded-md border border-slate-200 bg-slate-50 px-3 py-2 font-mono text-xs text-slate-600"
        >
          {t("preferences.example", preview)}
        </p>
      </CardContent>

      <CardFooter className="justify-end gap-2 border-t border-slate-200 pt-6">
        <Button
          variant="outline"
          onClick={() => setDraft(stated)}
          disabled={!dirty || saving}
        >
          {t("common.reset")}
        </Button>
        <Button onClick={save} disabled={!dirty || saving}>
          {saving ? t("common.saving") : t("common.save")}
        </Button>
      </CardFooter>
    </Card>
  );
}
