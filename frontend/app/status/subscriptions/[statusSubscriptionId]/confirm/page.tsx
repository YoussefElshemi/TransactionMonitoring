import { Suspense } from "react";

import { SubscriptionLanding } from "@/components/status/SubscriptionLanding";

/**
 * Where the confirmation link in the subscribe email lands.
 *
 * The `Suspense` boundary is required rather than decorative: the landing
 * component reads the token with `useSearchParams`, and Next refuses to
 * pre-render a page containing that hook unless a boundary tells it what to
 * show while the query string is unknown. Without one the build fails, which is
 * the good outcome — the bad one would be shipping a page that renders blank
 * for the one moment it exists for.
 */
export default function ConfirmSubscriptionPage() {
  return (
    <Suspense fallback={null}>
      <SubscriptionLanding action="confirm" />
    </Suspense>
  );
}
