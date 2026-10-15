"use client";

import { ApprovalsScreen } from "@/components/approvals/ApprovalsScreen";
import { useTranslation } from "@/context/LocalizationContext";

const MyApprovals = () => {
  const t = useTranslation();

  return (
  <ApprovalsScreen
    endpoint="/approvals/mine"
    eyebrow={t("approvals.fourEyes")}
    title={t("approvals.myRequests")}
    description={t("approvals.everythingYouHaveAskedSomebody")}
    showRequester={false}
    emptyMessage={(status) =>
      status === "Pending"
        ? "Nothing of yours is waiting on anybody"
        : "You have raised no requests"
    }
  />
  );
};

export default MyApprovals;
