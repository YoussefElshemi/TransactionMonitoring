"use client";

import { NotFound } from "@/components/NotFound";
import { useTranslation } from "@/context/LocalizationContext";

export default function NotFoundPage() {
  const t = useTranslation();
  return (
    <div className="flex min-h-screen items-center justify-center bg-slate-50">
      <NotFound
        title={t("app.thisPageDoesnTExist")}
        description={t("app.theLinkMayBeOut")}
        backHref="/"
        backLabel={t("app.goToTheDashboard")}
      />
    </div>
  );
}
