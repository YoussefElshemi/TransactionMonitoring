"use client";

import { useEffect, useState } from "react";

import { useLocalization, useTranslation } from "@/context/LocalizationContext";
import { InheritedSelect } from "@/components/preferences/InheritedSelect";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardFooter } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import {
  LocalizationCatalogue,
  StatedPreferences,
  fetchLocalizationCatalogue,
  localeLabel,
  timeZoneLabel,
} from "@/lib/localization";

export function OrganizationDefaultsPanel({
  defaultLocale,
  defaultTimeZone,
  saving,
  onSave,
}: {
  defaultLocale: string | null;
  defaultTimeZone: string | null;
  saving: boolean;
  onSave: (patch: Record<string, string | null>) => void;
}) {
  const t = useTranslation();
  const { locale } = useLocalization();

  const [catalogue, setCatalogue] = useState<LocalizationCatalogue | null>(null);
  const [catalogueFailed, setCatalogueFailed] = useState(false);

  const saved: StatedPreferences = {
    locale: defaultLocale,
    timeZone: defaultTimeZone,
  };
  const [draft, setDraft] = useState<StatedPreferences>(saved);

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
    setDraft({ locale: defaultLocale, timeZone: defaultTimeZone });
  }, [defaultLocale, defaultTimeZone]);

  const patch: Record<string, string | null> = {};
  if (draft.locale !== saved.locale) patch.defaultLocale = draft.locale;
  if (draft.timeZone !== saved.timeZone) patch.defaultTimeZone = draft.timeZone;
  const dirty = Object.keys(patch).length > 0;

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

  if (!catalogue) {
    return (
      <Card>
        <CardContent className="space-y-4 pt-6">
          {Array.from({ length: 2 }).map((_, i) => (
            <Skeleton key={i} className="h-16 w-full" />
          ))}
        </CardContent>
      </Card>
    );
  }

  return (
    <Card className="flex flex-col">
      <CardContent className="flex-1 space-y-6 pt-6">
        <div>
          <h2 className="text-sm font-semibold text-slate-900">
            {t("organization.defaults.title")}
          </h2>
          <p className="mt-0.5 text-sm leading-relaxed text-slate-500">
            {t("organization.defaults.description")}
          </p>
        </div>

        <InheritedSelect
          label={t("organization.defaults.locale.label")}
          value={draft.locale}
          inheritedValue={catalogue.defaultLocale}
          inheritedFrom={t("organization.defaults.inherited")}
          options={catalogue.locales}
          optionLabel={(tag) => localeLabel(tag, locale)}
          onChange={(next) =>
            setDraft((current) => ({ ...current, locale: next }))
          }
          disabled={saving}
        />

        <InheritedSelect
          label={t("organization.defaults.timeZone.label")}
          value={draft.timeZone}
          inheritedValue={catalogue.defaultTimeZone}
          inheritedFrom={t("organization.defaults.inherited")}
          options={catalogue.timeZones}
          optionLabel={(zone) => timeZoneLabel(zone, locale)}
          onChange={(next) =>
            setDraft((current) => ({ ...current, timeZone: next }))
          }
          disabled={saving}
        />
      </CardContent>

      <CardFooter className="justify-end gap-2 border-t border-slate-200 pt-6">
        <Button
          variant="outline"
          onClick={() => setDraft(saved)}
          disabled={!dirty || saving}
        >
          {t("common.reset")}
        </Button>
        <Button onClick={() => onSave(patch)} disabled={!dirty || saving}>
          {saving ? t("common.saving") : t("common.save")}
        </Button>
      </CardFooter>
    </Card>
  );
}
