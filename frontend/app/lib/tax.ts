import type { TaxTreatment } from "@/types";

export const TAX_TREATMENT_LABELS: Record<TaxTreatment, string> = {
  Standard: "Standard",
  ReverseCharge: "Reverse charge",
  OutOfScope: "Out of scope",
};

export const TAX_TREATMENT_HINTS: Record<TaxTreatment, string> = {
  Standard:
    "Tax charged on the document at the rate stated, and accounted for by the platform. This is the ordinary answer for a customer in the issuing entity's own country, and for a customer elsewhere in the EU who has produced no confirmed registration.",
  ReverseCharge:
    "No tax charged, because the customer accounts for it themselves under their own registration. Only reachable for a business elsewhere in the EU whose registration somebody has confirmed — the confirmation is what moves the liability, not the number being on file.",
  OutOfScope:
    "No tax charged, because the supply falls outside the scope of the issuing entity's tax altogether. The ordinary answer for a customer outside the EU, and the one an entity with no registered jurisdiction reaches for everybody.",
};

export const REVERSE_CHARGE_EXPLAINED =
  "Reverse charge does not mean no tax was due. It means the customer declares it under their own registration instead of paying it to the platform — so the zero on the document is the point of it, and the line saying so is what their finance team needs in order to account for it.";

export const VERIFICATION_MOVES_THE_TAX =
  "A registration that has not been verified does not move the tax. The invoice will charge tax, because the supplier that accepted an invented number is the one that owes the tax on it. Verifying is a separate act, and until it is done the number on file changes nothing.";

export const VERIFICATION_IS_STRUCTURAL =
  "Verifying checks that the number is the shape a registration takes in that country. A number that passes could still belong to somebody else; a number that fails certainly belongs to nobody, and accepting it would put reverse charge on a document with nothing behind it.";

export const INVALID_TAX_IDENTIFIER_MESSAGE =
  "That is not the shape a tax registration takes in the country on this profile. Check the number against the customer's own paperwork, and check the country with it — a registration is verified against the country it was issued in, not the one the customer trades from.";

export const CHANGE_CLEARS_VERIFICATION =
  "Saving this will un-verify the registration, because what was confirmed was the number against the country and one of the two is changing. Until somebody verifies it again the next invoice charges tax rather than reverse-charging it.";

export interface TaxJurisdiction {
  countryCode: string;
  name: string;
  isEuMember: boolean;
  standardRate: number;
}

export const TAX_JURISDICTIONS: TaxJurisdiction[] = [
  { countryCode: "GB", name: "United Kingdom", isEuMember: false, standardRate: 20 },
  { countryCode: "AT", name: "Austria", isEuMember: true, standardRate: 20 },
  { countryCode: "BE", name: "Belgium", isEuMember: true, standardRate: 21 },
  { countryCode: "BG", name: "Bulgaria", isEuMember: true, standardRate: 20 },
  { countryCode: "CY", name: "Cyprus", isEuMember: true, standardRate: 19 },
  { countryCode: "CZ", name: "Czechia", isEuMember: true, standardRate: 21 },
  { countryCode: "DE", name: "Germany", isEuMember: true, standardRate: 19 },
  { countryCode: "DK", name: "Denmark", isEuMember: true, standardRate: 25 },
  { countryCode: "EE", name: "Estonia", isEuMember: true, standardRate: 22 },
  { countryCode: "ES", name: "Spain", isEuMember: true, standardRate: 21 },
  { countryCode: "FI", name: "Finland", isEuMember: true, standardRate: 25.5 },
  { countryCode: "FR", name: "France", isEuMember: true, standardRate: 20 },
  { countryCode: "GR", name: "Greece", isEuMember: true, standardRate: 24 },
  { countryCode: "HR", name: "Croatia", isEuMember: true, standardRate: 25 },
  { countryCode: "HU", name: "Hungary", isEuMember: true, standardRate: 27 },
  { countryCode: "IE", name: "Ireland", isEuMember: true, standardRate: 23 },
  { countryCode: "IT", name: "Italy", isEuMember: true, standardRate: 22 },
  { countryCode: "LT", name: "Lithuania", isEuMember: true, standardRate: 21 },
  { countryCode: "LU", name: "Luxembourg", isEuMember: true, standardRate: 17 },
  { countryCode: "LV", name: "Latvia", isEuMember: true, standardRate: 21 },
  { countryCode: "MT", name: "Malta", isEuMember: true, standardRate: 18 },
  { countryCode: "NL", name: "Netherlands", isEuMember: true, standardRate: 21 },
  { countryCode: "PL", name: "Poland", isEuMember: true, standardRate: 23 },
  { countryCode: "PT", name: "Portugal", isEuMember: true, standardRate: 23 },
  { countryCode: "RO", name: "Romania", isEuMember: true, standardRate: 19 },
  { countryCode: "SE", name: "Sweden", isEuMember: true, standardRate: 25 },
  { countryCode: "SI", name: "Slovenia", isEuMember: true, standardRate: 22 },
  { countryCode: "SK", name: "Slovakia", isEuMember: true, standardRate: 23 },
];

export const jurisdiction = (countryCode: string): TaxJurisdiction | null =>
  TAX_JURISDICTIONS.find(
    (candidate) => candidate.countryCode === countryCode.trim().toUpperCase()
  ) ?? null;

export const isSupportedJurisdiction = (countryCode: string): boolean =>
  jurisdiction(countryCode) !== null;

export const countryName = (countryCode: string): string =>
  jurisdiction(countryCode)?.name ?? countryCode;

// Greece uses EL for VAT registrations and GR for addresses.
export const registrationPrefix = (countryCode: string): string =>
  countryCode.trim().toUpperCase() === "GR"
    ? "EL"
    : countryCode.trim().toUpperCase();

export const JURISDICTION_LIST_HINT =
  "The United Kingdom and the twenty-seven EU members — the jurisdictions this platform holds rules for. A country outside the list is refused rather than defaulted, because a supply the platform cannot decide the tax on is one it cannot honestly put a figure for on a document.";

export const predictTreatment = (
  supplierCountry: string,
  customerCountry: string,
  hasVerifiedRegistration: boolean
): TaxTreatment => {
  const supplier = jurisdiction(supplierCountry);
  const customer = jurisdiction(customerCountry);

  if (supplier === null) return "OutOfScope";

  if (customer !== null && supplier.countryCode === customer.countryCode) {
    return "Standard";
  }

  if (customer !== null && customer.isEuMember) {
    if (hasVerifiedRegistration) return "ReverseCharge";
    if (supplier.isEuMember) return "Standard";
  }

  return "OutOfScope";
};

export const predictRate = (
  supplierCountry: string,
  customerCountry: string,
  hasVerifiedRegistration: boolean
): number => {
  const treatment = predictTreatment(
    supplierCountry,
    customerCountry,
    hasVerifiedRegistration
  );

  if (treatment !== "Standard") return 0;

  const supplier = jurisdiction(supplierCountry);
  const customer = jurisdiction(customerCountry);

  return supplier?.countryCode === customer?.countryCode
    ? (supplier?.standardRate ?? 0)
    : (customer?.standardRate ?? 0);
};
