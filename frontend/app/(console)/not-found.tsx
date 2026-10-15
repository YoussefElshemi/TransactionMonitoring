"use client";

import { NotFound } from "@/components/NotFound";
import { useTranslation } from "@/context/LocalizationContext";

export default function ConsoleNotFoundPage() {
  const t = useTranslation();
  return (
    <NotFound
      title={t("notFound.thisPageDoesnTExist")}
      description={t("notFound.theLinkMayBeOut")}
      backHref="/"
      backLabel={t("notFound.goToDashboard")}
    />
  );
}
