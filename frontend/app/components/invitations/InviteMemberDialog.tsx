"use client";

import { useEffect, useState } from "react";

import { InvitableRole } from "@/types";
import { OFFERABLE_ROLES, ROLE_OFFERS } from "@/lib/invitations";
import { useSubOrganizations } from "@/lib/roles";
import { usePermissions } from "@/context/PermissionsContext";
import { useTranslation } from "@/context/LocalizationContext";
import { cn } from "@/lib/utils";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";

const NO_UNIT = "__none__";

export interface InviteRequest {
  email: string;
  role: InvitableRole;
  subOrganizationId: string | null;
}

export function InviteMemberDialog({
  open,
  onOpenChange,
  organizationId,
  defaultRole = "User",
  titleKey = "invitations.invite.title",
  descriptionKey = "invitations.invite.description",
  submitting,
  onSubmit,
}: {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  organizationId: string;
  defaultRole?: InvitableRole;
  titleKey?: string;
  descriptionKey?: string;
  submitting: boolean;
  onSubmit: (request: InviteRequest) => void;
}) {
  const t = useTranslation();
  const { holds } = usePermissions();

  const [email, setEmail] = useState("");
  const [role, setRole] = useState<InvitableRole>(defaultRole);
  const [unit, setUnit] = useState<string>(NO_UNIT);

  const units = useSubOrganizations(
    organizationId,
    open && holds("sub-organizations:read")
  );

  useEffect(() => {
    if (!open) return;
    setEmail("");
    setRole(defaultRole);
    setUnit(NO_UNIT);
  }, [open, defaultRole]);

  const submit = () => {
    onSubmit({
      email: email.trim(),
      role,
      subOrganizationId: unit === NO_UNIT ? null : unit,
    });
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>{t(titleKey)}</DialogTitle>
          <DialogDescription>{t(descriptionKey)}</DialogDescription>
        </DialogHeader>

        <div className="space-y-4">
          <div className="space-y-1.5">
            <label
              htmlFor="invite-email"
              className="font-mono text-xs uppercase tracking-widest text-slate-400"
            >
              {t("common.email")}
            </label>
            <Input
              id="invite-email"
              type="email"
              autoComplete="off"
              value={email}
              onChange={(event) => setEmail(event.target.value)}
              disabled={submitting}
              onKeyDown={(event) => {
                if (event.key !== "Enter" || !email.trim() || submitting) return;
                event.preventDefault();
                submit();
              }}
            />
          </div>

          <div
            role="radiogroup"
            aria-labelledby="invite-role-label"
            className="space-y-1.5"
          >
            <span
              id="invite-role-label"
              className="block font-mono text-xs uppercase tracking-widest text-slate-400"
            >
              {t("invitations.invite.role")}
            </span>
            <div className="space-y-2">
              {OFFERABLE_ROLES.map((offerable) => {
                const offer = ROLE_OFFERS[offerable];
                const selected = role === offerable;

                return (
                  <label
                    key={offerable}
                    className={cn(
                      "flex cursor-pointer items-start gap-3 rounded-lg border px-3 py-2.5 transition-colors",
                      selected
                        ? "border-brand-600 bg-brand-50"
                        : "border-slate-200 hover:bg-slate-50"
                    )}
                  >
                    <input
                      type="radio"
                      name="invite-role"
                      value={offerable}
                      checked={selected}
                      disabled={submitting}
                      onChange={() => setRole(offerable)}
                      className="mt-1 size-4 flex-shrink-0 accent-brand-600"
                    />
                    <span className="min-w-0">
                      <span className="block font-mono text-sm text-slate-900">
                        {t(offer.nameKey)}
                      </span>
                      <span className="block text-xs text-slate-500">
                        {t(offer.descriptionKey)}
                      </span>
                    </span>
                  </label>
                );
              })}
            </div>
          </div>

          {units.data.length > 0 && (
            <div className="space-y-1.5">
              <label
                htmlFor="invite-unit"
                className="font-mono text-xs uppercase tracking-widest text-slate-400"
              >
                {t("invitations.invite.businessUnit")}
              </label>
              <Select
                value={unit}
                onValueChange={setUnit}
                disabled={submitting}
              >
                <SelectTrigger id="invite-unit">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value={NO_UNIT}>
                    {t("invitations.invite.wholeOrganisation")}
                  </SelectItem>
                  {units.data.map((candidate) => (
                    <SelectItem
                      key={candidate.subOrganizationId}
                      value={candidate.subOrganizationId}
                    >
                      {candidate.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
              <p className="text-xs text-slate-500">
                {t("invitations.invite.businessUnitHelp")}
              </p>
            </div>
          )}
        </div>

        <DialogFooter>
          <Button
            variant="outline"
            onClick={() => onOpenChange(false)}
            disabled={submitting}
          >
            {t("common.cancel")}
          </Button>
          <Button onClick={submit} disabled={submitting || email.trim() === ""}>
            {submitting
              ? t("invitations.invite.sending")
              : t("invitations.invite.send")}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
