"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { useParams, useSearchParams } from "next/navigation";
import { CheckCircle2, CircleAlert, Loader2 } from "lucide-react";

import { statusFetch } from "@/lib/statusClient";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { useTranslation } from "@/context/LocalizationContext";

export type SubscriptionAction = "confirm" | "unsubscribe";

interface Copy {
  working: string;
  successTitle: string;
  successBody: string;
  failureTitle: string;
  failureBody: string;
  missingTokenBody: string;
}

const COPY: Record<SubscriptionAction, Copy> = {
  confirm: {
    working: "Confirming your subscription…",
    successTitle: "You are subscribed",
    successBody:
      "You will get an email when an incident is opened or updated, and when maintenance is scheduled. Every message carries a one-click unsubscribe link.",
    failureTitle: "That confirmation link did not work",
    failureBody:
      "It may have already been used, or it may have expired. Subscribing again from the status page will send a fresh one.",
    missingTokenBody:
      "This link is missing its token, which usually means it was copied out of an email rather than clicked. Open the link from the message itself, or subscribe again from the status page.",
  },
  unsubscribe: {
    working: "Unsubscribing…",
    successTitle: "You have been unsubscribed",
    successBody:
      "No further status emails will be sent to that address. You can subscribe again at any time from the status page — nothing here stops you.",
    failureTitle: "That unsubscribe link did not work",
    failureBody:
      "It may have already been used, in which case the address is already off the list and nothing more is needed. If you are still receiving messages, use the link in the most recent one.",
    missingTokenBody:
      "This link is missing its token, which usually means it was copied out of an email rather than clicked. Use the unsubscribe link in the footer of any status message you have received.",
  },
};

type Outcome = "working" | "done" | "failed" | "no-token";

export function SubscriptionLanding({ action }: { action: SubscriptionAction }) {
  const t = useTranslation();
  const params = useParams<{ statusSubscriptionId: string }>();
  const searchParams = useSearchParams();
  const [outcome, setOutcome] = useState<Outcome>("working");

  const subscriptionId = params?.statusSubscriptionId ?? "";
  const token = searchParams.get("token") ?? "";
  const copy = COPY[action];

  useEffect(() => {
    if (token.length === 0 || subscriptionId.length === 0) {
      setOutcome("no-token");
      return;
    }

    // Strict mode mounts effects twice in development. The second run would
    // send a token the first has already spent, and the user would be shown
    // the failure of a request that succeeded.
    let fired = false;

    const run = async () => {
      if (fired) return;
      fired = true;

      try {
        const response = await statusFetch(
          `/status/subscriptions/${encodeURIComponent(
            subscriptionId
          )}/${action}?token=${encodeURIComponent(token)}`,
          { method: "GET" }
        );

        setOutcome(response.ok ? "done" : "failed");
      } catch {
        setOutcome("failed");
      }
    };

    run();

    return () => {
      fired = true;
    };
  }, [action, subscriptionId, token]);

  if (outcome === "working") {
    return (
      <Card>
        <CardContent className="flex items-center gap-3 p-6">
          <Loader2 className="size-5 flex-shrink-0 animate-spin text-slate-400" />
          <p className="text-sm text-slate-600">{copy.working}</p>
        </CardContent>
      </Card>
    );
  }

  const succeeded = outcome === "done";

  return (
    <Card
      className={
        succeeded
          ? "border-settled-200 bg-settled-50"
          : "border-attention-200 bg-attention-50"
      }
    >
      <CardContent className="flex items-start gap-4 p-6">
        {succeeded ? (
          <CheckCircle2 className="mt-0.5 size-6 flex-shrink-0 text-settled-700" />
        ) : (
          <CircleAlert className="mt-0.5 size-6 flex-shrink-0 text-attention-700" />
        )}
        <div className="min-w-0">
          <h1
            className={`font-mono text-lg font-medium tracking-tight ${
              succeeded ? "text-settled-700" : "text-attention-700"
            }`}
          >
            {succeeded ? copy.successTitle : copy.failureTitle}
          </h1>
          <p className="mt-2 max-w-xl text-sm leading-relaxed text-slate-600">
            {succeeded
              ? copy.successBody
              : outcome === "no-token"
                ? copy.missingTokenBody
                : copy.failureBody}
          </p>
          <Button variant="outline" className="mt-5" asChild>
            <Link href="/status">{t("status.backToTheStatusPage")}</Link>
          </Button>
        </div>
      </CardContent>
    </Card>
  );
}
