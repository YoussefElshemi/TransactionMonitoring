"use client";

import { useState } from "react";
import { toast } from "sonner";

import { Party, PartyResource } from "@/types";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import { API_BASE, reportApiError, unwrap } from "@/lib/api";
import { shortId } from "@/lib/resources";
import { MAX_RISK_NOTE } from "@/lib/parties";
import { RiskScore } from "@/components/parties/PartyParts";
import { cn } from "@/lib/utils";

import { apiFetch } from "@/lib/http";

import { LocalTime } from "@/components/LocalTime";
import { useTranslation } from "@/context/LocalizationContext";

export function RiskAssessmentPanel({
  party,
  currentUserId,
  canAssess,
  onAssessed,
  onUnauthorized,
}: {
  party: Party;
  currentUserId: string | null | undefined;
  canAssess: boolean;
  onAssessed: (resource: PartyResource) => void;
  onUnauthorized: () => void;
}) {
  const t = useTranslation();
  const [editing, setEditing] = useState<boolean>(false);
  const [withdrawing, setWithdrawing] = useState<boolean>(false);
  const [score, setScore] = useState<string>(
    party.riskScore === null ? "" : String(party.riskScore)
  );
  const [note, setNote] = useState<string>("");
  const [saving, setSaving] = useState<boolean>(false);
  const [scoreError, setScoreError] = useState<string | null>(null);
  const [noteError, setNoteError] = useState<string | null>(null);

  const assessed = party.riskScore !== null;

  const open = (mode: "set" | "withdraw") => {
    setEditing(true);
    setWithdrawing(mode === "withdraw");
    setScore(party.riskScore === null ? "" : String(party.riskScore));
    setNote("");
    setScoreError(null);
    setNoteError(null);
  };

  const close = () => {
    setEditing(false);
    setWithdrawing(false);
    setScoreError(null);
    setNoteError(null);
  };

  const submit = async () => {
    const trimmedNote = note.trim();
    let invalid = false;

    if (!trimmedNote) {
      setNoteError(
        withdrawing
          ? "Say why the assessment is being withdrawn. Taking a judgement back is a judgement, and it is the only record of one."
          : "Say what this score is based on. It is the whole of the record behind the number."
      );
      invalid = true;
    } else if (trimmedNote.length > MAX_RISK_NOTE) {
      setNoteError(
        `Keep this to ${MAX_RISK_NOTE} characters. It is ${trimmedNote.length}.`
      );
      invalid = true;
    } else {
      setNoteError(null);
    }

    let riskScore: number | null = null;

    if (!withdrawing) {
      const parsed = Number(score.trim());
      if (
        score.trim() === "" ||
        !Number.isInteger(parsed) ||
        parsed < 0
      ) {
        setScoreError("A score is a whole number, zero or above.");
        invalid = true;
      } else {
        setScoreError(null);
        riskScore = parsed;
      }
    }

    if (invalid) return;

    setSaving(true);

    try {
      const response = await apiFetch(
        `${API_BASE}/parties/${party.partyId}/risk`,
        {
          method: "PUT",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ riskScore, note: trimmedNote }),
          credentials: "include",
        }
      );

      if (response.ok) {
        onAssessed(await unwrap<PartyResource>(response));
        close();
        toast.success(
          withdrawing ? t("parties.riskAssessmentWithdrawn") : t("parties.riskAssessmentRecorded"),
          {
            description: withdrawing
              ? t("parties.partyHasNoScoreAgain")
              : t("parties.recordedAgainstYourAccountReason"),
          }
        );
        return;
      }

      await reportApiError(response, {
        fallback: t("parties.failedRecordRiskAssessment"),
        onUnauthorized,
        fieldLabels: { "Body.RiskScore": "Score", "Body.Note": "Reason" },
      });
    } catch {
      toast.error(t("parties.failedRecordRiskAssessment"));
    } finally {
      setSaving(false);
    }
  };

  return (
    <Card>
      <CardContent className="space-y-4 pt-6">
        <div className="flex flex-wrap items-start justify-between gap-3">
          <div className="min-w-0">
            <h2 className="text-sm font-semibold text-slate-900">
              {t("parties.riskAssessment")}
            </h2>
            <p className="mt-0.5 max-w-xl text-sm text-slate-500">
              A judgement about this party, on your organisation&apos;s own
              scale. Rules can read it — and a party nobody has assessed has no
              score at all, which is not the same as a score of zero: a rule
              reading an unset score does not fire.
            </p>
          </div>
          {!editing && canAssess && (
            <div className="flex flex-shrink-0 items-center gap-2">
              <Button size="sm" onClick={() => open("set")}>
                {assessed ? "Revise" : "Assess"}
              </Button>
              {assessed && (
                <Button
                  variant="ghost"
                  size="sm"
                  className="text-critical-500 hover:text-critical-700"
                  onClick={() => open("withdraw")}
                >
                  {t("parties.withdraw")}
                </Button>
              )}
            </div>
          )}
        </div>

        {!editing && (
          <div className="space-y-3">
            <div className="flex flex-wrap items-center gap-3">
              <RiskScore score={party.riskScore} className="text-sm" />
              {assessed && party.riskSetAt && (
                <span className="text-xs text-slate-400">
                  <LocalTime value={party.riskSetAt} />
                  {party.riskSetByUserId && (
                    <>
                      {" · "}
                      {party.riskSetByUserId === currentUserId
                        ? "you"
                        : shortId(party.riskSetByUserId)}
                    </>
                  )}
                </span>
              )}
            </div>

            {party.riskNote ? (
              <blockquote className="border-l-2 border-slate-200 pl-3 text-sm leading-relaxed text-slate-700">
                {party.riskNote}
              </blockquote>
            ) : assessed ? (
              <p className="text-sm text-slate-400">
                {t("parties.noReasonWasRecordedWith")}
              </p>
            ) : (
              <p className="text-sm text-slate-400">
                Nobody has assessed this party.
                {!canAssess &&
                  " A manager or an admin records an assessment."}
              </p>
            )}
          </div>
        )}

        {editing && (
          <div className="space-y-4 rounded-lg border border-slate-200 bg-slate-50/60 p-3">
            {withdrawing ? (
              <p className="text-sm text-slate-600">
                Withdrawing leaves this party with no score. Any rule reading one
                stops firing on them — which may be exactly the intent, and is
                worth being sure of.
              </p>
            ) : (
              <div className="space-y-1.5">
                <label className="font-mono text-xs uppercase tracking-widest text-slate-400">
                  {t("parties.score")}
                </label>
                <Input
                  type="number"
                  min={0}
                  step={1}
                  value={score}
                  disabled={saving}
                  onChange={(event) => {
                    setScore(event.target.value);
                    setScoreError(null);
                  }}
                  className={cn(
                    "w-32 font-mono",
                    scoreError && "border-critical-500"
                  )}
                />
                {scoreError ? (
                  <p className="text-xs text-critical-700">{scoreError}</p>
                ) : (
                  <p className="text-xs text-slate-400">
                    A whole number, zero or above. What it means is your
                    organisation&apos;s to decide — the rules that read it are
                    written against the same scale.
                  </p>
                )}
              </div>
            )}

            <div className="space-y-1.5">
              <label className="flex items-baseline gap-2 font-mono text-xs uppercase tracking-widest text-slate-400">
                {t("common.reason")}
                <span className="normal-case tracking-normal text-attention-700">
                  required
                </span>
              </label>
              <Textarea
                rows={3}
                value={note}
                disabled={saving}
                maxLength={MAX_RISK_NOTE}
                placeholder={
                  withdrawing
                    ? t("parties.whyAssessmentNoLongerStands")
                    : t("parties.whatScoreBased")
                }
                onChange={(event) => {
                  setNote(event.target.value);
                  setNoteError(null);
                }}
                className={cn(noteError && "border-critical-500")}
              />
              <div className="flex flex-wrap items-baseline justify-between gap-2">
                {noteError ? (
                  <p className="text-xs text-critical-700">{noteError}</p>
                ) : (
                  <p className="text-xs text-slate-400">
                    Recorded against your account in this party&apos;s history,
                    alongside the score, and readable by the next person to open
                    them.
                  </p>
                )}
                <span className="font-mono text-[11px] text-slate-400 tabular-nums">
                  {note.trim().length} / {MAX_RISK_NOTE}
                </span>
              </div>
            </div>

            <div className="flex flex-wrap items-center gap-2">
              <Button size="sm" onClick={submit} disabled={saving}>
                {saving
                  ? "Saving..."
                  : withdrawing
                    ? "Withdraw assessment"
                    : "Record assessment"}
              </Button>
              <Button
                variant="outline"
                size="sm"
                onClick={close}
                disabled={saving}
              >
                {t("common.cancel")}
              </Button>
            </div>
          </div>
        )}
      </CardContent>
    </Card>
  );
}
