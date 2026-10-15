"use client";

import { LocalTime } from "@/components/LocalTime";
import { cn } from "@/lib/utils";
import { useTranslation } from "@/context/LocalizationContext";

const isRecord = (value: unknown): value is Record<string, unknown> =>
  value !== null && typeof value === "object" && !Array.isArray(value);

const ACRONYMS = new Set([
  "id", "iban", "bic", "bin", "url", "uri", "api", "ip", "iso", "uuid",
  "msisdn", "kyc", "aml", "vat", "mcc", "sepa", "swift", "utc", "pan", "otp",
  "cvv", "sku", "dob", "ssn", "tin", "lei",
]);

export const humaniseKey = (key: string): string => {
  const words = key
    // camelCase first, then ACRONYMFollowedByWord — that order matters, since
    // the second split needs the first to have left `IBANNumber` intact.
    .replace(/([a-z0-9])([A-Z])/g, "$1 $2")
    .replace(/([A-Z]+)([A-Z][a-z])/g, "$1 $2")
    .replace(/[_\-.]+/g, " ")
    .trim()
    .split(/\s+/)
    .filter(Boolean);

  if (words.length === 0) return key;

  return words
    .map((word, position) => {
      if (ACRONYMS.has(word.toLowerCase())) return word.toUpperCase();
      if (word.length > 1 && word === word.toUpperCase()) return word;

      const lower = word.toLowerCase();
      return position === 0
        ? lower.charAt(0).toUpperCase() + lower.slice(1)
        : lower;
    })
    .join(" ");
};

const elementLabel = (key: string, index: number): string => {
  const label = humaniseKey(key);

  const singular = label.endsWith("ies")
    ? `${label.slice(0, -3)}y`
    : /(ss|x|z|ch|sh)es$/.test(label)
      ? label.slice(0, -2)
      : label.endsWith("ss") || !label.endsWith("s")
        ? label
        : label.slice(0, -1);

  return `${singular} ${index + 1}`;
};

const ISO_INSTANT =
  /^\d{4}-\d{2}-\d{2}[T ]\d{2}:\d{2}(:\d{2})?(\.\d+)?(Z|[+-]\d{2}:?\d{2})?$/;
const ISO_DATE = /^\d{4}-\d{2}-\d{2}$/;

const isMasked = (value: string): boolean => /^•+$/.test(value);

const isMachineString = (key: string, value: string): boolean =>
  /(^|[^a-z])id$/i.test(key) ||
  /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(value) ||
  /^[0-9A-Z]{12,}$/.test(value);

const CURRENCY_KEYS = ["currency", "currencyCode", "currency_code", "ccy"];
const AMOUNT_KEY = /(amount|total|balance|fee|price|sum|value)$/i;

const siblingCurrency = (siblings: Record<string, unknown>): string | null => {
  for (const key of CURRENCY_KEYS) {
    const value = siblings[key];
    if (typeof value === "string" && /^[A-Za-z]{3}$/.test(value)) {
      return value.toUpperCase();
    }
  }
  return null;
};

const formatNumber = (
  key: string,
  value: number,
  siblings: Record<string, unknown>
): string => {
  // A numeric id is a name rather than a quantity: `1042` must not become
  // `1,042`.
  if (/(^|[^a-z])id$/i.test(key) || !Number.isFinite(value)) {
    return String(value);
  }

  if (AMOUNT_KEY.test(key)) {
    const currency = siblingCurrency(siblings);

    if (currency) {
      try {
        return new Intl.NumberFormat(undefined, {
          style: "currency",
          currency,
        }).format(value);
      } catch {
        return `${new Intl.NumberFormat().format(value)} ${currency}`;
      }
    }
  }

  return new Intl.NumberFormat(undefined, { maximumFractionDigits: 20 }).format(
    value
  );
};

function Absent({ children }: { children: React.ReactNode }) {
  return <span className="text-console-600">{children}</span>;
}

function ScalarValue({
  name,
  value,
  siblings,
}: {
  name: string;
  value: unknown;
  siblings: Record<string, unknown>;
}) {
  const t = useTranslation();
  if (value === null || value === undefined) {
    return <Absent>{t("payloadFields.notProvided")}</Absent>;
  }

  if (typeof value === "boolean") {
    return <span className="text-console-200">{value ? "Yes" : "No"}</span>;
  }

  if (typeof value === "number") {
    return (
      <span className="font-mono text-console-200">
        {formatNumber(name, value, siblings)}
      </span>
    );
  }

  if (typeof value === "string") {
    if (value === "") return <Absent>{t("payloadFields.empty")}</Absent>;

    if (isMasked(value)) {
      return (
        <span
          className="font-mono text-console-500"
          title={t("payloadFields.withheldThisIsNotThe")}
        >
          {value}
        </span>
      );
    }

    if (ISO_INSTANT.test(value)) {
      return <LocalTime value={value} className="text-console-200" />;
    }

    if (ISO_DATE.test(value)) {
      // Printed as written. `Date` would read it as UTC midnight and shift it
      // a day backwards for every reader west of Greenwich.
      return <span className="text-console-200">{value}</span>;
    }

    return (
      <span
        className={cn(
          "text-console-200",
          isMachineString(name, value) && "font-mono text-[13px]"
        )}
      >
        {value}
      </span>
    );
  }

  return <span className="font-mono text-console-200">{String(value)}</span>;
}

const MAX_DEPTH = 4;

function Line({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div className="flex flex-wrap items-baseline justify-between gap-x-6 gap-y-0.5 py-1.5">
      <dt className="text-sm text-console-400">{label}</dt>
      <dd className="min-w-0 break-words text-right text-sm">{children}</dd>
    </div>
  );
}

function Group({
  label,
  children,
}: {
  label: string;
  children: React.ReactNode;
}) {
  return (
    <div className="py-1.5">
      <p className="font-mono text-[11px] uppercase tracking-widest text-console-500">
        {label}
      </p>
      <div className="mt-0.5 border-l border-white/10 pl-3">{children}</div>
    </div>
  );
}

function Row({
  name,
  value,
  siblings,
  depth,
}: {
  name: string;
  value: unknown;
  siblings: Record<string, unknown>;
  depth: number;
}) {
  const t = useTranslation();
  const label = humaniseKey(name);

  if (Array.isArray(value)) {
    if (value.length === 0) {
      return (
        <Line label={label}>
          <Absent>{t("payloadFields.none")}</Absent>
        </Line>
      );
    }

    if (value.every((item) => !isRecord(item) && !Array.isArray(item))) {
      return (
        <Line label={label}>
          <span className="flex flex-wrap justify-end gap-1.5">
            {value.map((item, index) => (
              <span key={index} className="rounded bg-white/5 px-1.5 py-0.5">
                <ScalarValue name={name} value={item} siblings={siblings} />
              </span>
            ))}
          </span>
        </Line>
      );
    }

    return (
      <Group label={`${label} · ${value.length}`}>
        {value.map((item, index) => (
          <Group key={index} label={elementLabel(name, index)}>
            <Fields value={item} depth={depth + 2} />
          </Group>
        ))}
      </Group>
    );
  }

  if (isRecord(value)) {
    if (Object.keys(value).length === 0) {
      return (
        <Line label={label}>
          <Absent>{t("payloadFields.nothing")}</Absent>
        </Line>
      );
    }

    return (
      <Group label={label}>
        <Fields value={value} depth={depth + 1} />
      </Group>
    );
  }

  return (
    <Line label={label}>
      <ScalarValue name={name} value={value} siblings={siblings} />
    </Line>
  );
}

function Fields({ value, depth }: { value: unknown; depth: number }) {
  if (depth >= MAX_DEPTH) {
    return (
      <pre className="overflow-x-auto py-1.5 font-mono text-xs text-console-300">
        {JSON.stringify(value, null, 2)}
      </pre>
    );
  }

  if (Array.isArray(value)) {
    return (
      <dl className="divide-y divide-white/5">
        {value.map((item, index) => (
          <Row
            key={index}
            name={`item ${index + 1}`}
            value={item}
            siblings={{}}
            depth={depth}
          />
        ))}
      </dl>
    );
  }

  if (!isRecord(value)) {
    return (
      <div className="py-1.5 text-sm">
        <ScalarValue name="" value={value} siblings={{}} />
      </div>
    );
  }

  return (
    <dl className="divide-y divide-white/5">
      {Object.entries(value).map(([key, child]) => (
        <Row key={key} name={key} value={child} siblings={value} depth={depth} />
      ))}
    </dl>
  );
}

export function PayloadFields({ payload }: { payload: unknown }) {
  const t = useTranslation();
  const empty =
    (isRecord(payload) && Object.keys(payload).length === 0) ||
    (Array.isArray(payload) && payload.length === 0);

  if (empty) {
    return (
      <p className="px-4 py-3 text-sm text-console-500">
        {t("payloadFields.thisPayloadCarriesNoFields")}
      </p>
    );
  }

  return (
    <div className="px-4 py-2">
      <Fields value={payload} depth={0} />
    </div>
  );
}
