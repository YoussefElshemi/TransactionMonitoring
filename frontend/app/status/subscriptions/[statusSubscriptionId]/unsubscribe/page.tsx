import { Suspense } from "react";

import { SubscriptionLanding } from "@/components/status/SubscriptionLanding";

export default function UnsubscribePage() {
  return (
    <Suspense fallback={null}>
      <SubscriptionLanding action="unsubscribe" />
    </Suspense>
  );
}
