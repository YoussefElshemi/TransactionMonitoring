import type { PaymentMethod, PaymentMethodKind } from "@/types";

export const PAYMENT_METHOD_KIND_HINTS: Record<PaymentMethodKind, string> = {
  Card: "payments.kindHint.card",
  DirectDebit: "payments.kindHint.directDebit",
  BankTransfer: "payments.kindHint.bankTransfer",
};

export const paymentMethodKindHintKey = (
  kind: PaymentMethodKind | string
): string => PAYMENT_METHOD_KIND_HINTS[kind as PaymentMethodKind] ?? "";

export const EXPIRY_WARNING_DAYS = 60;

const MILLISECONDS_PER_DAY = 24 * 60 * 60 * 1000;

export const daysUntilExpiry = (
  method: Pick<PaymentMethod, "expiresAt">,
  now: Date = new Date()
): number | null => {
  if (method.expiresAt === null) return null;

  const expires = new Date(method.expiresAt).getTime();
  if (Number.isNaN(expires)) return null;

  return Math.floor((expires - now.getTime()) / MILLISECONDS_PER_DAY);
};

export const hasExpired = (
  method: Pick<PaymentMethod, "expiresAt">,
  now: Date = new Date()
): boolean => {
  const days = daysUntilExpiry(method, now);
  return days !== null && days < 0;
};

export const isExpiringSoon = (
  method: Pick<PaymentMethod, "expiresAt">,
  now: Date = new Date()
): boolean => {
  const days = daysUntilExpiry(method, now);
  return days !== null && days >= 0 && days <= EXPIRY_WARNING_DAYS;
};

export const needsAttention = (
  method: Pick<PaymentMethod, "expiresAt">,
  now: Date = new Date()
): boolean => hasExpired(method, now) || isExpiringSoon(method, now);

export const EXPIRING_SOON_MESSAGE = "payments.expiringSoon";

export const EXPIRED_MESSAGE = "payments.expired";

export const NOTHING_ON_FILE_HEADLINE = "payments.nothingOnFileHeadline";

export const NOTHING_ON_FILE_DETAIL = "payments.nothingOnFileDetail";

export const ADDING_HAPPENS_AT_THE_PROVIDER = "payments.addingAtTheProvider";

export const REMOVING_HAPPENS_AT_THE_PROVIDER =
  "payments.removingAtTheProvider";

export const isAbsoluteHttpsUrl = (url: string): boolean => {
  try {
    return new URL(url).protocol === "https:";
  } catch {
    return false;
  }
};

export const SESSION_URLS_MUST_BE_HTTPS = "payments.sessionUrlsMustBeHttps";

export const methodsInReadingOrder = (
  methods: readonly PaymentMethod[],
  now: Date = new Date()
): PaymentMethod[] =>
  [...methods].sort((left, right) => {
    if (left.isDefault !== right.isDefault) return left.isDefault ? -1 : 1;

    const leftNeeds = needsAttention(left, now);
    const rightNeeds = needsAttention(right, now);
    if (leftNeeds !== rightNeeds) return leftNeeds ? -1 : 1;

    return left.label.localeCompare(right.label);
  });
