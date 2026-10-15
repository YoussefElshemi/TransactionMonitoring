"use client";

import { useState } from "react";
import { Check, Copy } from "lucide-react";
import { toast } from "sonner";

import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { ToneBadge } from "@/components/status/StatusChips";
import { NO_SOURCE_RANGES } from "@/lib/webhooks";
import {
  invalidateWebhookCatalogue,
  useWebhookCatalogue,
} from "@/lib/useWebhookCatalogue";
import { useTranslation } from "@/context/LocalizationContext";

export function WebhookSendingFromCard({
  pinnedVersion,
  className,
}: {
  pinnedVersion?: number;
  className?: string;
}) {
  const t = useTranslation();
  const { status, catalogue } = useWebhookCatalogue();
  const [copied, setCopied] = useState<boolean>(false);

  if (status === "loading") {
    return (
      <Card className={className}>
        <CardContent className="space-y-3 pt-6">
          <Skeleton className="h-4 w-32" />
          <Skeleton className="h-4 w-full" />
          <Skeleton className="h-4 w-2/3" />
        </CardContent>
      </Card>
    );
  }

  if (status === "unavailable" || catalogue === null) {
    return (
      <Card className={className}>
        <CardContent className="space-y-3 pt-6">
          <h2 className="font-mono text-sm font-medium text-slate-900">
            {t("webhooks.sendingFrom")}
          </h2>
          <p className="max-w-prose text-sm text-slate-500">
            The published contract could not be read, so the source ranges and
            payload versions are not available here.
          </p>
          <Button
            variant="outline"
            size="sm"
            onClick={() => invalidateWebhookCatalogue()}
          >
            {t("common.retry")}
          </Button>
        </CardContent>
      </Card>
    );
  }

  const ranges = catalogue.sourceIpRanges;

  const copyRanges = async () => {
    try {
      await navigator.clipboard.writeText(ranges.join("\n"));
      setCopied(true);
      setTimeout(() => setCopied(false), 1500);
    } catch {
      toast.error(t("common.copyingIsnTAvailableBrowser"));
    }
  };

  return (
    <Card className={className}>
      <CardContent className="space-y-5 pt-6">
        <div>
          <h2 className="font-mono text-sm font-medium text-slate-900">
            {t("webhooks.sendingFrom")}
          </h2>
          <p className="mt-1 max-w-prose text-sm text-slate-500">
            {t("webhooks.whatToTellWhoeverRuns")}
          </p>
        </div>

        <div className="space-y-2">
          <div className="flex flex-wrap items-center justify-between gap-2">
            <p className="font-mono text-[11px] uppercase tracking-widest text-slate-400">
              {t("webhooks.sourceIpRanges")}
            </p>
            {ranges.length > 0 && (
              <Button
                variant="ghost"
                size="sm"
                className="h-auto gap-1.5 px-1.5 py-0.5 text-xs text-slate-500"
                onClick={copyRanges}
              >
                {copied ? (
                  <Check className="size-3.5 text-settled-500" />
                ) : (
                  <Copy className="size-3.5" />
                )}
                Copy all
              </Button>
            )}
          </div>

          {ranges.length === 0 ? (
            <p className="max-w-prose text-sm leading-relaxed text-slate-500">
              {NO_SOURCE_RANGES}
            </p>
          ) : (
            <div className="flex flex-wrap gap-1.5">
              {ranges.map((range) => (
                <code
                  key={range}
                  className="rounded-md border border-slate-200 bg-slate-50 px-2 py-1 font-mono text-xs text-slate-700"
                >
                  {range}
                </code>
              ))}
            </div>
          )}
        </div>

        <div className="space-y-2 border-t border-slate-100 pt-4">
          <p className="font-mono text-[11px] uppercase tracking-widest text-slate-400">
            {t("webhooks.payloadVersions")}
          </p>
          <div className="flex flex-wrap items-center gap-1.5">
            {catalogue.supportedPayloadVersions.map((version) => {
              const isCurrent = version === catalogue.currentPayloadVersion;
              const isPinned = version === pinnedVersion;

              return (
                <ToneBadge
                  key={version}
                  tone={isPinned ? "inflight" : isCurrent ? "settled" : "inert"}
                  title={
                    isPinned
                      ? t("webhooks.versionEndpointPinned")
                      : isCurrent
                        ? t("webhooks.whatNewEndpointGetsUnless")
                        : t("webhooks.stillUnderstoodStillDeliveredEndpoints")
                  }
                >
                  v{version}
                  {isPinned ? " · this endpoint" : isCurrent ? " · current" : ""}
                </ToneBadge>
              );
            })}
          </div>
          <p className="max-w-prose text-xs leading-relaxed text-slate-500">
            A version stays supported until it is retired with notice. An
            endpoint pinned to an older one goes on receiving that envelope, so a
            new version never arrives at a parser that has not been updated for
            it.
          </p>
        </div>
      </CardContent>
    </Card>
  );
}
