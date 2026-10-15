"use client";

import {
  ProblemDetails,
  RuleBuilderSubmitProps,
  RuleBuilderSubmitRuleProps,
  RuleResource,
  SchemaViolation,
  ValidationProblemDetails
} from "@/types";
import RuleBuilder from "@/components/RuleBuilder";
import { useParams, usePathname, useSearchParams } from "next/navigation";
import { useRouter } from "next/navigation";
import { toast } from "sonner";
import { useEffect, useState } from "react";
import { usePermissions } from "@/context/PermissionsContext";
import { PageHeader } from "@/components/PageHeader";
import { Skeleton } from "@/components/ui/skeleton";
import { loginHref } from "@/lib/authRedirect";
import { API_BASE, reportApiError, unwrap } from "@/lib/api";

import { apiFetch } from "@/lib/http";
import { useTranslation } from "@/context/LocalizationContext";

const CreateRule = () => {
  const t = useTranslation();
  const [loading, setLoading] = useState<boolean>(true);
  const [schemaViolations, setSchemaViolations] = useState<SchemaViolation[]>(
    []
  );

  const router = useRouter();
  const { id } = useParams();
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const { holdsOver, resolved } = usePermissions();

  const redirectUri = searchParams.toString()
    ? `${pathname}?${searchParams.toString()}`
    : pathname;

  const fetchRuleSetById = async (id: string) => {
    setLoading(true);

    try {
      const response = await apiFetch(`${API_BASE}/rule-sets/${id}`, {
        method: "GET",
        headers: {
          "Content-Type": "application/json"
        },
        credentials: "include"
      });

      if (!response.ok) {
        if (response.status === 400) {
          const errorData = (await response.json()) as ValidationProblemDetails;
          toast.error(
            Object.keys(errorData.errors)
              .map(
                (key) =>
                  `${key}${key.length ? ": " : ""}${errorData.errors[key].join("\n")}`
              )
              .join("\n")
          );
        } else if (response.status === 401) {
          toast.error(t("common.pleaseLoginAccessResource"), {
            id: "unauthorized"
          });
          return router.push(loginHref(redirectUri));
        } else if (response.status === 403) {
          const errorData = (await response.json()) as ProblemDetails;
          toast.error(
            errorData.detail ||
              t("common.youDoNotHavePermission"),
            {
              id: "forbidden"
            }
          );
          return router.push("/");
        } else {
          const errorData = (await response.json()) as ProblemDetails;
          toast.error(
            errorData.detail || t("ruleSets.errorOccurredFetchingRuleSet"),
            { id: "error-fetching-rule-sets" }
          );
        }
      }
    } catch {
      toast.error(t("ruleSets.errorOccurredFetchingRuleSet"), {
        id: "error-fetching-rule-sets"
      });
    } finally {
      setLoading(false);
    }
  };

  const canWriteRules = holdsOver("rules:write", { ruleSetId: id as string });

  useEffect(() => {
    if (!resolved || canWriteRules) return;
    toast.error(t("ruleSets.authoringRuleNeedsRulesWrite"));
    router.push(`/rule-sets/${id}`);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [resolved, canWriteRules, id]);

  useEffect(() => {
    fetchRuleSetById(id as string);
  }, [id]);

  const handleCreateRule = async ({
    ruleSetId,
    rules,
    newFriendlyName,
    isNewEnabled,
    newMatchPolarity,
    newAction,
    newSeverity,
    newRiskWeight,
    newHoldPolicy,
    newAlertPolicy,
    setIsCreateModalOpen
  }: RuleBuilderSubmitRuleProps) => {
    try {
      if (!rules.length) {
        return toast.error(t("common.thereMustRootRule"));
      }

      if (rules.length > 1) {
        return toast.error(t("common.thereCanOnlyOneRoot"));
      }

      // Built key by key rather than spread: an unset hold field has to be
      // left off the body entirely, because on create as on patch these two
      // are the only fields where "absent" and "null" are not the same thing.
      const payload: Record<string, unknown> = {
        friendlyName: newFriendlyName,
        ruleBody: rules[0],
        isEnabled: isNewEnabled,
        matchPolarity: newMatchPolarity,
        action: newAction,
        severity: newSeverity,
        riskWeight: newRiskWeight
      };
      if (newHoldPolicy.holdWindow !== null) {
        payload.holdWindow = newHoldPolicy.holdWindow;
      }
      if (newHoldPolicy.holdExpiryVerdict !== null) {
        payload.holdExpiryVerdict = newHoldPolicy.holdExpiryVerdict;
      }
      if (newAlertPolicy.alertSubjectRole !== null) {
        payload.alertSubjectRole = newAlertPolicy.alertSubjectRole;
      }
      if (newAlertPolicy.deduplicationWindow !== null) {
        payload.deduplicationWindow = newAlertPolicy.deduplicationWindow;
      }

      const response = await apiFetch(
        `${API_BASE}/rule-sets/${ruleSetId}/rules`,
        {
          method: "POST",
          headers: {
            "Content-Type": "application/json"
          },
          body: JSON.stringify(payload),
          credentials: "include"
        }
      );

      if (response.ok) {
        toast.success(t("ruleSets.successfullyCreatedRule"));
        const data = await unwrap<RuleResource>(response);
        setSchemaViolations([]);
        setIsCreateModalOpen(false);
        router.push(`/rules/${data.rule.ruleId}`);
        return;
      }

      await reportApiError(response, {
        fallback: t("ruleSets.failedCreateRule"),
        onUnauthorized: () => router.push(loginHref(redirectUri)),
        onSchemaViolations: (report) => {
          setSchemaViolations(report.violations);
          setIsCreateModalOpen(false);
        },
      });
    } catch {
      toast.error(t("ruleSets.failedCreateRule"));
    }
  };

  const handleCreate = async ({
    setIsCreateModalOpen,
    setNewFriendlyName,
    setNewIsEnabled
  }: RuleBuilderSubmitProps) => {
    setIsCreateModalOpen(true);
    setNewFriendlyName("");
    setNewIsEnabled(true);
  };

  if (loading) return (
    <div className="space-y-4">
      <Skeleton className="h-8 w-48" />
      <Skeleton className="h-64 w-full" />
    </div>
  );

  return (
    <div className="space-y-6">
      <PageHeader eyebrow={t("ruleSets.ruleSet")} title={t("ruleSets.createRule")} />
      <RuleBuilder
        ruleSetId={id as string}
        initialRules={[]}
        submitRuleText={t("ruleSets.create")}
        readOnly={false}
        canEditMatchPolarity
        schemaViolations={schemaViolations}
        handleSubmit={handleCreate}
        handleSubmitRule={handleCreateRule}
      />
    </div>
  );
};

export default CreateRule;
