"use client";

import { ApprovalsScreen } from "@/components/approvals/ApprovalsScreen";
import { useTranslation } from "@/context/LocalizationContext";

const Approvals = () => {
  const t = useTranslation();

  return (
  <ApprovalsScreen
    endpoint="/approvals"
    eyebrow={t("approvals.fourEyes")}
    title={t("approvals.approvals")}
    description={t("approvals.everythingWaitingOnASecond")}
    emptyMessage={(status) =>
      status === "Pending"
        ?
          "Nothing waiting for review"
        : "No requests found"
    }
  />
  );
};

export default Approvals;
