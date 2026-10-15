import { ReactNode } from "react";

export interface PagedMetadata {
  pageNumber: number;
  pageSize: number;
  pageCount: number;
  totalCount: number;
  hasMore: boolean;
}

/** @deprecated Use {@link PagedMetadata}. Kept for the `List*` aliases below. */
type Metadata = PagedMetadata;

// Relation keys are omitted (never null) when the caller may not read that record.
// id null = no record; id set + key absent = not disclosed. See lib/resources.ts.

export interface ValidationError {
  [key: string]: string[];
}

export interface SandboxCreditResponse {
  organizationId: string;
  allowance: number;
  used: number;
  remaining: number;
  resetsAt: string;
  unit: string;
}

export interface SandboxResetCount {
  resource: string;
  count: number;
}

export interface SandboxResetResponse {
  organizationId: string;
  mode: string;
  counts: SandboxResetCount[];
  totalDeleted: number;
  resetAt: string;
}

export interface SimulationResponse {
  simulationId: string;
  organizationId: string;
  scenario: string;
  status: string;
  totalPayments: number;
  processedCount: number;
  seed: number;
  createdByUserId: string;
  completedAt: string | null;
  failureReason: string | null;
  createdAt: string;
  updatedAt: string;
  mode: string;
}

export interface SimulationResource { simulation: SimulationResponse }

export interface WebhookDeliveryResponse {
  outboxMessageId: string;
  organizationId: string | null;
  webhookConfigurationId: string | null;
  event: string | null;
  payloadVersion: number | null;
  destination: string;
  payload: string;
  attempts: number;
  lastAttemptedAt: string | null;
  responseStatusCode: number | null;
  responseBody: string | null;
  latencyMilliseconds: number | null;
  isDelivered: boolean;
  deliveredAt: string | null;
  suppressedAt: string | null;
  replayOfOutboxMessageId: string | null;
  createdAt: string;
  updatedAt: string;
  mode: string;
}

export interface WebhookDeliveryResource {
  webhookDelivery: WebhookDeliveryResponse;
  organization?: Organization;
}

export interface WebhookReplayBatch {
  organizationId: string;
  from: string;
  to: string;
  matched: number;
  queued: number;
  hasMore: boolean;
}

export interface ValidationProblemDetails {
  detail: string;
  errors: ValidationError;
}

export interface ProblemDetails {
  detail: string;
  errors: ValidationError;
}

export interface SchemaViolation {
  field: string;
  message: string;
}

export interface SchemaViolationProblemDetails {
  type?: string;
  title?: string;
  detail?: string;
  status?: number;
  violations: SchemaViolation[];
}

export interface ApiClient {
  apiClientId: string;
  clientId: string;
  friendlyName: string;
  isEnabled: boolean;
  scopes: string[];
  organizationId: string;
  createdByUserId: string;
  createdAt: string;
  updatedAt: string;
  mode?: string;
}

export interface CreatedApiClient extends ApiClient {
  clientSecret: string;
}

export interface ApiClientSecret {
  apiClientSecretId: string;
  apiClientId: string;
  secretPrefix: string;
  expiresAt: string | null;
  revokedAt: string | null;
  lastUsedAt: string | null;
  createdAt: string;
  updatedAt: string;
  mode?: string;
}

export interface CreatedApiClientSecret extends ApiClientSecret {
  clientSecret: string;
}

export interface TransactionSummary {
  transactionId: string;
  friendlyName: string;
  externalReference: string | null;
  payload: Record<string, unknown>;
  /** True on every read endpoint (scalars come back as bullets); false only from `/reveal`. */
  payloadMasked: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface Transaction extends TransactionSummary {
  organizationId: string;
  ruleEvaluations: RuleEvaluation[];
  cases: Case[];
  decision: Decision | null;
  parties: TransactionParty[];
}

export type DecisionStatus =
  | "Pending"
  | "Held"
  | "Released"
  | "Blocked"
  | "Expired";

export type DecisionReasonCode =
  | "NoRuleMatched"
  | "RuleAllowed"
  | "RuleFlagged"
  | "RuleReviewRequired"
  | "RuleBlocked"
  | "AnalystApproved"
  | "AnalystRejected"
  | "HoldExpired";

export type HoldExpiryVerdict = "Release" | "Expire" | "Block" | "Never";

export interface Decision {
  decisionId: string;
  transactionId: string;
  transactionReference: string | null;
  status: DecisionStatus | (string & {});
  reasonCode: DecisionReasonCode | (string & {});
  caseId: string | null;
  resolvedByUserId: string | null;
  firedRules: RuleEvaluation[];
  heldAt: string | null;
  expiresAt: string | null;
  holdExpiryVerdict: HoldExpiryVerdict | (string & {}) | null;
  resolvedAt: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface ListTransaction {
  metadata: Metadata;
  data: TransactionSummary[];
}

export type CaseStatus =
  | "Pending"
  | "Assigned"
  | "Approved"
  | "Rejected"
  | "Expired"
  | "InReview"
  | "Escalated"
  | "PendingInformation"
  | "OnHold";

export type CaseDisposition =
  | "TruePositive"
  | "FalsePositive"
  | "TrueNegative"
  | "FalseNegative";

export interface Case {
  caseId: string;
  friendlyName: string;
  status: CaseStatus | (string & {});
  severity: RuleSeverity;
  transactionId: string | null;
  partyId: string | null;
  ruleEvaluations: RuleEvaluation[];
  alerts: Alert[];
  organizationId: string;
  disposition: CaseDisposition | null;
  reasonCode: CaseReasonCode | null;
  reopenedAt: string | null;
  resolvedAt: string | null;
  assignedAt: string | null;
  dueAt: string;
  /** Set by an escalation rule; unrelated to `status === "Escalated"`. */
  escalatedAt: string | null;
  escalationRuleId: string | null;
  assignedToUserId: string | null;
  queueId: string | null;
  queueName: string | null;
  updatedAt: string;
  createdAt: string;
}

export type CaseAgeBucket = "Fresh" | "Ageing" | "DueSoon" | "Breached";

// Server-computed against one instant per response, and frozen once resolved.
// Don't recompute the bucket from dueAt and Date.now(); see lib/ageing.
export interface CaseAgeing {
  ageInMilliseconds: number;
  timeToDueInMilliseconds: number;
  bucket: CaseAgeBucket | (string & {});
  isBreached: boolean;
}

export type AlertStatus = "Open" | "Suppressed" | "Closed";

export const ALERT_STATUSES: AlertStatus[] = ["Open", "Suppressed", "Closed"];

export type AlertSuppressionReason = "DuplicateInWindow";

export interface Alert {
  alertId: string;
  friendlyName: string;
  status: AlertStatus | (string & {});
  severity: RuleSeverity;
  organizationId: string;
  ruleId: string;
  ruleVersionId: string;
  ruleEvaluationId: string;
  transactionId: string;
  partyId: string | null;
  partyRole: PartyRole | (string & {}) | null;
  caseId: string | null;
  suppressionReason: AlertSuppressionReason | (string & {}) | null;
  suppressedByAlertId: string | null;
  raisedAt: string;
  closedAt: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface ConditionMatch {
  field: string;
  condition: RuleCondition | (string & {}) | null;
  quantifier?: RuleQuantifier | (string & {}) | null;
  expected: unknown | null;
  actual: unknown | null;
  // Whether this leaf contributed to the rule firing, with not/MatchesWhenFalse folded in —
  // so it can be true when the raw comparison failed. Use describeCondition to phrase it.
  matched: boolean;
}

export interface RuleEvaluation {
  ruleEvaluationId: string;
  transactionId: string;
  matched: boolean;
  ruleId: string;
  ruleVersion: RuleVersion;
  conditionMatches: ConditionMatch[] | null;
  organizationId: string;
  updatedAt: string;
  createdAt: string;
  mode?: string;
}

export interface BackTest {
  backTestId: string;
  ruleId: string;
  ruleVersionId: string;
  status: "Pending" | "Running" | "Completed" | "Failed";
  sampleSize: number;
  totalEvaluations: number;
  processedCount: number;
  createdByUserId: string;
  createdAt: string;
  updatedAt: string;
  completedAt: string | null;
  mode?: string;
}

export interface BackTestResult {
  backTestResultId: string;
  ruleEvaluationId: string;
  payload: Record<string, unknown>;
  payloadMasked: boolean;
  originalMatched: boolean;
  newMatched: boolean;
  hasChanged: boolean;
  createdAt: string;
}

export interface ListBackTestResult {
  metadata: Metadata;
  data: BackTestResult[];
}

export interface CaseNote {
  caseNoteId: string;
  caseId: string;
  organizationId: string;
  message: string;
  createdByUserId: string;
  updatedAt: string;
  createdAt: string;
}

export type RuleAction = "Allow" | "Flag" | "Review" | "Block";

export type RuleSeverity = "Info" | "Low" | "Medium" | "High" | "Critical";

// Whether the rule fires when its body is true or false. Fixed at creation.
// Migrated rules are MatchesWhenFalse, so never assume the default.
export type RuleMatchPolarity = "MatchesWhenTrue" | "MatchesWhenFalse";

export interface RuleSet {
  ruleSetId: string;
  friendlyName: string;
  ruleCount: number;
  organizationId: string;
  isEnabled: boolean;
  reviewThreshold: number | null;
  createdByUserId: string;
  createdAt: string;
  updatedAt: string;
}

export interface Rule {
  ruleId: string;
  ruleSetId: string;
  organizationId: string;
  friendlyName: string;
  ruleBody: RuleBodyDto;
  isEnabled: boolean;
  matchPolarity: RuleMatchPolarity;
  action: RuleAction;
  severity: RuleSeverity;
  riskWeight: number;
  holdWindow: number | null;
  holdExpiryVerdict: HoldExpiryVerdict | (string & {}) | null;
  // Null inherits the server default (Remitter). Optional because rule reads don't return it,
  // and undefined !== null.
  alertSubjectRole?: PartyRole | (string & {}) | null;
  /** Minutes. Null inherits (organisation, then 60). Not returned on reads; normalise with `?? null`. */
  deduplicationWindow?: number | null;
  createdByUserId: string;
  createdAt: string;
  updatedAt: string;
  isDeleted: boolean;
}

export interface RuleVersion {
  ruleVersionId: string;
  ruleId: string;
  versionNumber: number;
  ruleBody: RuleBodyDto;
  matchPolarity: RuleMatchPolarity;
  action: RuleAction;
  severity: RuleSeverity;
  riskWeight: number;
  ruleSetId: string;
  createdAt: string;
}

export interface ListRuleVersion {
  metadata: Metadata;
  data: RuleVersion[];
}

export type DeploymentStrategy = "AllAtOnce" | "Gradual";

export type DeploymentStatus =
  | "PendingApproval"
  | "InProgress"
  | "Completed"
  | "Cancelled"
  | "Failed";

export interface RuleDeployment {
  ruleDeploymentId: string;
  ruleId: string;
  targetRuleVersionId: string;
  previousRuleVersionId: string | null;
  strategy: DeploymentStrategy;
  status: DeploymentStatus;
  currentPercentage: number;
  stepPercentage: number | null;
  stepIntervalSeconds: number | null;
  createdByUserId: string;
  createdAt: string;
  updatedAt: string;
  completedAt: string | null;
}

export interface RuleTestResponse {
  matched: boolean;
  policy: Policy;
  outcome: Outcome;
}

export interface Policy {
  action: RuleAction;
  severity: RuleSeverity;
  riskWeight: number;
}

export interface Outcome {
  action: RuleAction;
  severity: RuleSeverity;
  riskScore: number;
}

export interface EvaluateRuleResponse {
  ruleId: string;
  friendlyName: string;
  matched: boolean;
  policy: Policy;
  outcome: Outcome | null;
  transactionId: string | null;
  transactionReference: string | null;
  case: Case | null;
  decision: Decision | null;
}

export interface EvaluateRuleSetResponse {
  ruleSetId: string;
  friendlyName: string;
  results: EvaluateRuleResponse[];
  outcome: Outcome;
  transactionId: string;
  transactionReference: string | null;
  case: Case | null;
  decision: Decision | null;
}

export interface EvaluateAllRulesResponse {
  results: EvaluateRuleResponse[];
  outcome: Outcome;
  transactionId: string;
  transactionReference: string | null;
  case: Case | null;
  decision: Decision | null;
}

export type RuleCondition =
  | "IsNull"
  | "EqualTo"
  | "GreaterThan"
  | "LessThan"
  | "GreaterThanEqual"
  | "LessThanEqual"
  | "In"
  | "Contains"
  | "StartsWith"
  | "EndsWith"
  | "NotEqualTo"
  | "NotIn"
  | "Matches"
  | "Between";

export type RuleQuantifier = "Any" | "All";

export type RuleFunction =
  | "Add"
  | "Subtract"
  | "Multiply"
  | "Divide"
  | "Modulo"
  | "Abs"
  | "Length"
  | "Count"
  | "Sum"
  | "Min"
  | "Max"
  | "Average"
  | "HourOfDay"
  | "MinuteOfDay"
  | "DayOfWeek"
  | "DaysSince"
  | "HoursSince";

export interface RuleExpressionDto {
  field?: string | null;
  party?: PartyNodeDto | null;
  velocity?: VelocityConditionDto | null;
  value?: unknown;
  function?: RuleFunction | (string & {}) | null;
  arguments?: RuleExpressionDto[] | null;
}

export interface RuleBodyDto {
  condition?: RuleCondition | null;
  field?: string | null;
  quantifier?: RuleQuantifier | (string & {}) | null;
  expression?: RuleExpressionDto | null;
  party?: PartyNodeDto | null;
  velocity?: VelocityConditionDto | null;
  value?: any; // eslint-disable-line @typescript-eslint/no-explicit-any
  and?: RuleBodyDto[] | null;
  or?: RuleBodyDto[] | null;
  not?: RuleBodyDto[] | null;
  json?: boolean | false;
}

export interface RuleBuilderProps {
  ruleSetId: string;
  initialRules: RuleBodyDto[];
  submitRuleText: string;
  readOnly: boolean;
  matchPolarity?: RuleMatchPolarity;
  canEditMatchPolarity?: boolean;
  saveButtonText?: string;
  allowVelocity?: boolean;
  schemaViolations?: SchemaViolation[];
  toolbarLeading?: ReactNode;
  toolbarActions?: ReactNode;
  handleSaveBody?: (rules: RuleBodyDto[]) => void | Promise<void>;
  handleSubmitRule?: (props: RuleBuilderSubmitRuleProps) => void;
  handleSubmit?: (props: RuleBuilderSubmitProps) => void;
}

export interface RuleBuilderSubmitRuleProps {
  ruleSetId: string;
  rules: RuleBodyDto[];
  newFriendlyName: string;
  isNewEnabled: boolean;
  newMatchPolarity: RuleMatchPolarity;
  newAction: RuleAction;
  newSeverity: RuleSeverity;
  newRiskWeight: number;
  newHoldPolicy: {
    holdWindow: number | null;
    holdExpiryVerdict: string | null;
  };
  newAlertPolicy: {
    alertSubjectRole: string | null;
    deduplicationWindow: number | null;
  };
  setIsCreateModalOpen: (state: boolean) => void;
}

export interface RuleBuilderSubmitProps {
  setIsCreateModalOpen: (state: boolean) => void;
  setNewFriendlyName: (state: string) => void;
  setNewIsEnabled: (state: boolean) => void;
}

export interface BreadCrumbProps {
  homeElement: ReactNode;
  separator: ReactNode;
  containerClasses?: string;
  listClasses?: string;
  activeClasses?: string;
  alternativeNames?: Record<string, string>;
}

export interface ApplicationUser {
  userId: string;
  roles: string[];
  email: string;
  userName: string;
  isEmailConfirmed: boolean;
  isEnabled: boolean;
  organizationId: string;
  subOrganizationId: string | null;
  createdAt: string;
  updatedAt: string;
  organization?: Organization;
}

export type OrganizationStatus = "Enabled" | "OverdueInvoice" | "Disabled";

export interface Organization {
  organizationId: string;
  friendlyName: string;
  status: OrganizationStatus;
  holdWindow: number | null;
  holdExpiryVerdict: HoldExpiryVerdict | (string & {}) | null;
  caseGroupingEnabled: boolean | null;
  deduplicationEnabled: boolean | null;
  caseGroupingPeriod: number | null;
  deduplicationWindow: number | null;
  caseGroupingScope: CaseGroupingScope | (string & {}) | null;
  caseGroupingSeverityPolicy: CaseGroupingSeverityPolicy | (string & {}) | null;
  defaultLocale: string | null;
  defaultTimeZone: string | null;
  maxAlertsPerCase: number | null;
  createdAt: string;
  updatedAt: string;
}

export type CaseGroupingScope = "Party" | "PartyAndRule";

export type CaseGroupingSeverityPolicy =
  | "Any"
  | "AtLeastAsSevere"
  | "SameSeverity";

export interface ListOrganization {
  metadata: Metadata;
  data: Organization[];
}

export type SchemaFieldType =
  | "String"
  | "Number"
  | "Boolean"
  | "DateTime"
  | "Object"
  | "Array";

export interface SchemaField {
  name: string;
  type: SchemaFieldType;
  isRequired: boolean;
  allowedValues?: string[];
  elementType?: SchemaFieldType;
  fields?: SchemaField[];
}

export interface TransactionSchema {
  transactionSchemaId: string;
  organizationId: string;
  fields: SchemaField[];
  currentVersionNumber: number;
  isDeleted: boolean;
  deletedAt: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface TransactionSchemaVersion {
  transactionSchemaVersionId: string;
  transactionSchemaId: string;
  organizationId: string;
  versionNumber: number;
  fields: SchemaField[];
  createdAt: string;
}

export type BillableResource =
  | "Unclassified"
  | "RuleEvaluation"
  | "RuleSetEvaluation"
  | "RuleTest"
  | "Backtest"
  | "SandboxWrite";

export type PricingModel = "Flat" | "Graduated" | "Volume";

export type CurrencyCode = "GBP" | "EUR" | "USD";

export type InvoiceStatus = "Draft" | "Issued";

export type TaxTreatment = "Standard" | "ReverseCharge" | "OutOfScope";

export interface DocumentParty {
  legalName: string;
  address: string;
  countryCode: string;
  taxIdentifier: string | null;
  email: string | null;
}

export type InvoiceLineType =
  | "Usage"
  | "PlatformFee"
  | "MinimumTopUp"
  | "Discount"
  | "CommitmentDrawdown"
  | "Adjustment"
  | "Tax";

export type DiscountType = "Percentage" | "FixedAmount";

export interface InvoiceLine {
  invoiceLineId: string;
  invoiceLineType: InvoiceLineType;
  billableResource: BillableResource | null;
  description: string;
  quantity: number;
  unitAmount: number;
  amount: number;
  invoiceAdjustmentId: string | null;
  sortOrder: number;
}

export interface Invoice {
  invoiceId: string;
  invoiceNumber: string | null;
  invoiceStatus: InvoiceStatus;
  billingEntityId: string | null;
  supplier: DocumentParty | null;
  customer: DocumentParty | null;
  currency: CurrencyCode;
  planCurrency: CurrencyCode;
  conversionRate: number | null;
  taxTreatment: TaxTreatment;
  taxRate: number;
  taxAmount: number;
  netAmount: number;
  issuedAt: string | null;
  dueAt: string | null;
  paymentTerms: number;
  planVersionId: string | null;
  planVersionNumber: number | null;
  costPerRequest: number;
  amount: number;
  month: number;
  year: number;
  organizationId: string;
  createdByUserId: string | null;
  isProcessed: boolean;
  processedAt: string | null;
  createdAt: string;
  updatedAt: string;
  lines: InvoiceLine[];
}

export interface Usage {
  usageId: string;
  resource: string;
  billableResource: BillableResource;
  count: number;
  createdAt: string;
}

export interface ListUsage {
  metadata: Metadata;
  data: Usage[];
}

export interface PlanPriceTier {
  planPriceTierId: string;
  fromQuantity: number;
  toQuantity: number | null;
  unitAmount: number;
  flatAmount: number;
}

export interface PlanPrice {
  planPriceId: string;
  planVersionId: string;
  billableResource: BillableResource | null;
  pricingModel: PricingModel;
  freeAllowance: number;
  tiers: PlanPriceTier[];
  createdAt: string;
}

export interface PlanVersion {
  planVersionId: string;
  planId: string;
  versionNumber: number;
  currency: CurrencyCode;
  costPerRequest: number;
  platformFee: number;
  minimumCharge: number;
  prices: PlanPrice[];
  publishedByUserId: string | null;
  createdAt: string;
}

export interface Plan {
  planId: string;
  name: string;
  description: string | null;
  organizationId: string | null;
  isEnabled: boolean;
  currentVersionNumber: number;
  currentVersion: PlanVersion | null;
  createdAt: string;
  updatedAt: string;
}

export interface Subscription {
  subscriptionId: string;
  organizationId: string;
  planVersionId: string;
  planVersion: PlanVersion | null;
  planName: string | null;
  effectiveFrom: string;
  effectiveTo: string | null;
  createdByUserId: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface ContractDiscount {
  contractDiscountId: string;
  organizationId: string;
  discountType: DiscountType;
  value: number;
  description: string;
  effectiveFrom: string;
  effectiveTo: string | null;
  createdByUserId: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface InvoiceAdjustment {
  invoiceAdjustmentId: string;
  organizationId: string;
  month: number;
  year: number;
  description: string;
  amount: number;
  appliedToInvoiceId: string | null;
  createdByUserId: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface Commitment {
  commitmentId: string;
  organizationId: string;
  amount: number;
  effectiveFrom: string;
  effectiveTo: string;
  drawn: number;
  remaining: number;
  createdByUserId: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface BillingEntity {
  billingEntityId: string;
  legalName: string;
  address: string;
  countryCode: string;
  taxIdentifier: string | null;
  invoiceNumberPrefix: string;
  creditNoteNumberPrefix: string;
  currency: CurrencyCode;
  isEnabled: boolean;
  createdByUserId: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface BillingProfile {
  billingProfileId: string;
  organizationId: string;
  billingEntityId: string;
  billingEntityLegalName: string | null;
  legalName: string;
  contactName: string;
  contactEmail: string;
  address: string;
  countryCode: string;
  taxIdentifier: string | null;
  taxIdentifierVerifiedAt: string | null;
  currency: CurrencyCode;
  paymentTerms: number;
  updatedByUserId: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface ExchangeRate {
  exchangeRateId: string;
  fromCurrency: CurrencyCode;
  toCurrency: CurrencyCode;
  rate: number;
  effectiveFrom: string;
  createdByUserId: string | null;
  createdAt: string;
}

export interface CreditNoteLine {
  creditNoteLineId: string;
  description: string;
  quantity: number;
  unitAmount: number;
  amount: number;
  sortOrder: number;
}

export interface CreditNote {
  creditNoteId: string;
  creditNoteNumber: string;
  invoiceId: string;
  invoiceNumber: string | null;
  organizationId: string;
  billingEntityId: string;
  reason: string;
  currency: CurrencyCode;
  netAmount: number;
  taxTreatment: TaxTreatment;
  taxRate: number;
  taxAmount: number;
  amount: number;
  issuedAt: string;
  createdByUserId: string | null;
  lines: CreditNoteLine[];
}

// Collection endpoints spell currencies "Gbp" (see lib/currency), and amounts are signed
// and never netted across currencies.

export type CollectedCurrency = "Gbp" | "Eur" | "Usd";

export type PaymentMethodKind = "Card" | "DirectDebit" | "BankTransfer";

export type PaymentStatus = "Pending" | "Settled" | "Failed" | "Returned";

export interface PaymentMethod {
  paymentMethodReference: string;
  paymentMethodKind: PaymentMethodKind;
  label: string;
  mandateReference: string | null;
  isDefault: boolean;
  expiresAt: string | null;
}

export interface PaymentMethodSession {
  url: string;
}

export interface Payment {
  paymentId: string;
  organizationId: string;
  amount: number;
  currency: CollectedCurrency;
  paymentStatus: PaymentStatus;
  paymentMethodKind: PaymentMethodKind;
  paymentMethodLabel: string | null;
  remittanceReference: string | null;
  receiptNumber: string | null;
  raisedForInvoiceId: string | null;
  receivedAt: string;
  settledAt: string | null;
  returnedAt: string | null;
  failureReason: string | null;
  recordedByUserId: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface InvoiceSettlement {
  invoiceId: string;
  amount: number;
  allocatedAmount: number;
  creditedAmount: number;
  outstandingAmount: number;
  isSettled: boolean;
}

export interface AccountBalance {
  organizationId: string;
  currency: CollectedCurrency;
  amount: number;
  entryCount: number;
}

export type AccountEntryReason =
  | "Receipt"
  | "Allocation"
  | "Refund"
  | "Return"
  | "CreditNote";

export interface AccountEntry {
  accountEntryId: string;
  organizationId: string;
  currency: CollectedCurrency;
  amount: number;
  reason: AccountEntryReason;
  paymentId: string | null;
  invoiceId: string | null;
  creditNoteId: string | null;
  description: string;
  createdByUserId: string | null;
  createdAt: string;
}

export type DunningStage =
  | "Grace"
  | "FirstNotice"
  | "SecondNotice"
  | "FinalNotice"
  | "RestrictionProposed";

export interface DunningState {
  dunningStateId: string;
  invoiceId: string;
  invoiceNumber: string | null;
  organizationId: string;
  stage: DunningStage;
  outstandingAmount: number;
  currency: CollectedCurrency | null;
  dueAt: string | null;
  nextAttemptAt: string;
  lastNoticedAt: string | null;
  collectionAttempts: number;
  lastAttemptedAt: string | null;
  restrictionProposedAt: string | null;
  resolvedAt: string | null;
  createdAt: string;
}

export interface CaseReasonCode {
  caseReasonCodeId: string;
  organizationId: string;
  code: string;
  description: string;
  disposition: CaseDisposition | null;
  isEnabled: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface CaseReasonCodeResource {
  caseReasonCode: CaseReasonCode;
  organization?: Organization;
}

export interface CaseReasonCodeUsage {
  caseReasonCodeId: string;
  code: string;
  caseCount: number;
}

export interface WebhookConfiguration {
  webhookConfigurationId: string;
  organizationId: string;
  endpointUrl: string;
  isEnabled: boolean;
  events: string[];
  payloadVersion: number;
  disabledAt: string | null;
  disabledReason: string | null;
  createdAt: string;
  updatedAt: string;
  mode: string;
}

export interface WebhookEndpointHealth {
  webhookConfigurationId: string;
  organizationId: string;
  isEnabled: boolean;
  consecutiveFailures: number;
  lastSucceededAt: string | null;
  lastFailedAt: string | null;
  disabledAt: string | null;
  disabledReason: string | null;
  from: string;
  to: string;
  delivered: number;
  failed: number;
  pending: number;
}

export type WebhookEventCategory =
  | "Case"
  | "Decision"
  | "Alert"
  | "Approval"
  | "SuspiciousActivityReport"
  | "Diagnostic";

export interface WebhookEventDefinition {
  event: string;
  category: WebhookEventCategory;
  description: string;
  isSubscribable: boolean;
  introducedInVersion: number;
}

export interface WebhookCatalogue {
  events: WebhookEventDefinition[];
  currentPayloadVersion: number;
  supportedPayloadVersions: number[];
  sourceIpRanges: string[];
}

// The signing secret is only returned here. Not a WebhookConfiguration: it lacks
// disabledAt/disabledReason, so spreading it over one would blank them.
export interface WebhookConfigurationSecret
  extends Omit<WebhookConfiguration, "disabledAt" | "disabledReason"> {
  secretKey: string;
}

export interface Statistic {
  case: CaseStatistic;
  usage: Record<string, number>;
}

export interface CaseStatistic {
  total: number;
  byStatus: Record<string, number>;
  averageResolutionTime?: number;
  breached: number;
  /** Already a percentage (0–100); don't multiply again. */
  breachRate: number;
}

export interface AuthContextType {
  logIn: (username: string, password: string) => Promise<void>;
  register: (
    username: string,
    email: string,
    password: string
  ) => Promise<void>;
  logOut: () => Promise<void>;
  fetchUser: () => Promise<ApplicationUser | null>;
  refreshUser: () => Promise<ApplicationUser | null>;
  user: ApplicationUser | null;
  authResolved: boolean;
}

export interface PaginationProps {
  handlePrevPage: () => void;
  pageNumber: number;
  totalPages: number;
  handleNextPage: () => void;
}

export interface StatisticCardProps {
  statisticName: string;
  statisticValue: string | number;
  colour?: string;
  hint?: string;
}

export type AuditEntityType =
  | "Role"
  | "SubOrganization"
  | "Rule"
  | "RuleSet"
  | "Transaction"
  | "Case"
  | "CaseNote"
  | "CaseReasonCode"
  | "ApiClient"
  | "WebhookConfiguration"
  | "Organization"
  | "ApplicationUser"
  | "TransactionSchema"
  | "PartyMapping"
  | "VelocityProfile"
  | "Party"
  | "Queue"
  | "EscalationRule"
  | "Approval"
  | "Attachment"
  | "CaseLink"
  | "NotificationSubscription"
  | "SuspiciousActivityReport";

export interface AuditLogChange {
  field: string;
  oldValue: unknown;
  newValue: unknown;
}

export interface AuditLog {
  auditLogId: string;
  entityType: AuditEntityType;
  entityId: string;
  organizationId: string | null;
  operation: "Created" | "Updated" | "Deleted" | "Revealed";
  changes: AuditLogChange[];
  changedByUserId: string | null;
  changedByApiClientId: string | null;
  sequence: number;
  previousHash: string | null;
  hash: string;
  createdAt: string;
}

// Returned as 200 whether or not the chain is intact; branch on isIntact.
export interface AuditChainVerification {
  organizationId: string;
  entryCount: number;
  anchorCount: number;
  isIntact: boolean;
  firstBrokenAuditLogId: string | null;
  firstBrokenSequence: number | null;
  firstBrokenAuditAnchorId: string | null;
  verifiedAt: string;
}

export interface Paged<T> {
  metadata: PagedMetadata;
  data: T[];
}

export interface RuleResource {
  rule: Rule;
  ruleSet?: RuleSet;
  organization?: Organization;
  createdByUser?: ApplicationUser;
}

export interface RuleSetResource {
  ruleSet: RuleSet;
  organization?: Organization;
  createdByUser?: ApplicationUser;
}

export interface RuleDeploymentResource {
  ruleDeployment: RuleDeployment;
  rule?: Rule;
  createdByUser?: ApplicationUser;
  targetRuleVersion?: RuleVersion;
  previousRuleVersion?: RuleVersion;
}

export interface RuleEvaluationResource {
  ruleEvaluation: RuleEvaluation;
  rule?: Rule;
  organization?: Organization;
  transaction?: TransactionSummary;
}

export interface CaseResource {
  case: Case;
  ageing: CaseAgeing;
  organization?: Organization;
  assignedToUser?: ApplicationUser;
  transaction?: TransactionSummary;
  rules?: Rule[];
}

export interface AlertResource {
  alert: Alert;
  organization?: Organization;
  rule?: Rule;
  transaction?: TransactionSummary;
}

export interface CaseNoteResource {
  caseNote: CaseNote;
  createdByUser?: ApplicationUser;
}

export interface TransactionResource {
  transaction: Transaction;
  organization?: Organization;
  rules?: Rule[];
  users?: ApplicationUser[];
}

export interface ApiClientResource {
  apiClient: ApiClient;
  organization?: Organization;
  createdByUser?: ApplicationUser;
}

export interface CreatedApiClientResource {
  apiClient: CreatedApiClient;
  organization?: Organization;
  createdByUser?: ApplicationUser;
}

export interface UserResource {
  user: ApplicationUser;
  organization?: Organization;
}

export interface InvoiceResource {
  invoice: Invoice;
  organization?: Organization;
  createdByUser?: ApplicationUser;
  settlement?: InvoiceSettlement;
}

export interface WebhookConfigurationResource {
  webhookConfiguration: WebhookConfiguration;
  organization?: Organization;
}

export interface TransactionSchemaResource {
  transactionSchema: TransactionSchema;
  organization?: Organization;
}

export interface TransactionSchemaVersionResource {
  transactionSchemaVersion: TransactionSchemaVersion;
  organization?: Organization;
}

export interface BackTestResource {
  backTest: BackTest;
  rule?: Rule;
  ruleVersion?: RuleVersion;
  createdByUser?: ApplicationUser;
}

export interface AuditLogResource {
  auditLog: AuditLog;
  changedByUser?: ApplicationUser;
  changedByApiClient?: ApiClient;
}

export interface EvaluateRuleResource {
  result: EvaluateRuleResponse;
  rules?: Rule[];
  users?: ApplicationUser[];
}

export interface EvaluateRuleSetResource {
  result: EvaluateRuleSetResponse;
  ruleSet?: RuleSet;
  rules?: Rule[];
  users?: ApplicationUser[];
}

export interface EvaluateAllRulesResource {
  result: EvaluateAllRulesResponse;
  rules?: Rule[];
  users?: ApplicationUser[];
}

export type PartyRole = "Remitter" | "Beneficiary";

export const PARTY_ROLES: PartyRole[] = [
  "Remitter",
  "Beneficiary"
];

export interface PartyAttributePath {
  name: string;
  path: string;
}

export interface PartyMappingEntry {
  role: PartyRole;
  path?: string | null;
  identifierPath: string;
  displayNamePath?: string | null;
  attributePaths?: PartyAttributePath[] | null;
}

export interface PartyMapping {
  partyMappingId: string;
  organizationId: string;
  mappings: PartyMappingEntry[];
  currentVersionNumber: number;
  isDeleted: boolean;
  deletedAt: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface PartyMappingVersion {
  partyMappingVersionId: string;
  partyMappingId: string;
  organizationId: string;
  versionNumber: number;
  mappings: PartyMappingEntry[];
  createdAt: string;
}

export interface PartyMappingUsage {
  ruleId: string;
  friendlyName: string;
  isEnabled?: boolean;
  roles: PartyRole[];
}

export interface PartyMappingInUseProblemDetails {
  type?: string;
  title?: string;
  detail?: string;
  status?: number;
  usages: PartyMappingUsage[];
}

export interface PartyAttribute {
  name: string;
  value: string;
}

export interface Party {
  partyId: string;
  organizationId: string;
  identifier: string;
  displayName: string | null;
  roles: PartyRole[];
  riskScore: number | null;
  riskNote: string | null;
  riskSetByUserId: string | null;
  riskSetAt: string | null;
  firstSeenAt: string;
  lastSeenAt: string;
  attributes: PartyAttribute[];
  createdAt: string;
  updatedAt: string;
}

export interface TransactionParty {
  transactionId: string;
  partyId: string;
  role: PartyRole;
  identifier: string;
  displayName?: string | null;
  createdAt: string;
}

export type PartyRuleAttribute =
  | "RiskScore"
  | "DaysSinceFirstSeen"
  | "DaysSinceLastSeen";

export const PARTY_RULE_ATTRIBUTES: PartyRuleAttribute[] = [
  "RiskScore",
  "DaysSinceFirstSeen",
  "DaysSinceLastSeen",
];

export interface PartyNodeDto {
  role: PartyRole | (string & {});
  attribute: PartyRuleAttribute | (string & {});
}

export interface PartyViolation {
  field: string;
  message: string;
}

export interface PartyRuleViolationProblemDetails {
  type?: string;
  title?: string;
  detail?: string;
  status?: number;
  violations: PartyViolation[];
}

export interface PartyResource {
  party: Party;
  organization?: Organization;
}

export interface PartyMappingResource {
  partyMapping: PartyMapping;
  organization?: Organization;
}

export interface PartyMappingVersionResource {
  partyMappingVersion: PartyMappingVersion;
  organization?: Organization;
}

export interface VelocityMetric {
  name: string;
  path: string;
}

export interface VelocityThreshold {
  name: string;
  value: number;
}

export interface VelocityProfile {
  velocityProfileId: string;
  organizationId: string;
  metrics: VelocityMetric[];
  thresholds: VelocityThreshold[];
  isDeleted: boolean;
  deletedAt: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface VelocityProfileUsage {
  ruleId: string;
  friendlyName: string;
  isEnabled?: boolean;
  metrics: string[];
  thresholds: string[];
}

export interface VelocityProfileInUseProblemDetails {
  type?: string;
  title?: string;
  detail?: string;
  status?: number;
  usages: VelocityProfileUsage[];
}

export type VelocityBackfillStatus =
  | "Pending"
  | "Running"
  | "Completed"
  | "Failed";

export interface VelocityBackfill {
  velocityBackfillId: string;
  organizationId: string;
  metrics: string[];
  status: VelocityBackfillStatus | (string & {});
  totalTransactions: number;
  processedCount: number;
  completedAt: string | null;
  createdAt: string;
  updatedAt: string;
}

export type VelocityFunction =
  | "Count"
  | "DistinctCount"
  | "Sum"
  | "Min"
  | "Max"
  | "Average"
  | "StructuringCount";

export const VELOCITY_FUNCTIONS: VelocityFunction[] = [
  "Count",
  "DistinctCount",
  "Sum",
  "Min",
  "Max",
  "Average",
  "StructuringCount",
];

export type VelocityWindowKind = "Rolling" | "Calendar";

export type VelocityWindowUnit = "Minute" | "Hour" | "Day" | "Week" | "Month";

export const VELOCITY_WINDOW_UNITS: VelocityWindowUnit[] = [
  "Minute",
  "Hour",
  "Day",
  "Week",
  "Month",
];

export interface VelocityWindowDto {
  kind: VelocityWindowKind | (string & {});
  unit: VelocityWindowUnit | (string & {});
  length?: number | null;
  timeZone?: string | null;
}

export interface VelocityConditionDto {
  role: PartyRole | (string & {});
  metric: string;
  function: VelocityFunction | (string & {});
  window: VelocityWindowDto;
  includeCurrent?: boolean | null;
  threshold?: string | null;
  band?: number | null;
}

export interface VelocityProfileResource {
  velocityProfile: VelocityProfile;
  organization?: Organization;
}

export interface VelocityBackfillResource {
  velocityBackfill: VelocityBackfill;
  organization?: Organization;
}

export type RoutingPolicy = "Manual" | "RoundRobin" | "LeastLoaded";

export const ROUTING_POLICIES: RoutingPolicy[] = [
  "Manual",
  "RoundRobin",
  "LeastLoaded",
];

export interface QueueMember {
  userId: string;
  userName: string | null;
  lastAssignedAt: string | null;
}

export interface Queue {
  queueId: string;
  organizationId: string;
  name: string;
  description: string | null;
  priority: number;
  routingPolicy: RoutingPolicy | (string & {});
  minimumSeverity: RuleSeverity | null;
  ruleIds: string[];
  matcher: RuleBodyDto | null;
  isEnabled: boolean;
  members: QueueMember[];
  createdAt: string;
  updatedAt: string;
}

export interface QueueResource {
  queue: Queue;
  organization?: Organization;
}

export interface QueueMemberLoad {
  userId: string;
  userName: string | null;
  openCaseCount: number;
  lastAssignedAt: string | null;
}

export interface QueueStatistic {
  queueId: string;
  name: string;
  priority: number;
  routingPolicy: RoutingPolicy | (string & {});
  isEnabled: boolean;
  depth: number;
  inProgressCount: number;
  oldestWaitingForSeconds: number | null;
  memberLoads: QueueMemberLoad[];
}

export interface QueueUsage {
  queueId: string;
  name: string;
  caseCount: number;
  openCaseCount: number;
  memberCount: number;
}

export interface SlaTarget {
  severity: RuleSeverity | (string & {});
  targetMinutes: number;
}

export interface EscalationRule {
  escalationRuleId: string;
  organizationId: string;
  name: string;
  description: string | null;
  priority: number;
  isEnabled: boolean;
  unassignedForMinutes: number | null;
  holdExpiryWithinMinutes: number | null;
  minimumSeverity: RuleSeverity | null;
  matcher: RuleBodyDto | null;
  targetQueueId: string | null;
  raiseSeverityTo: RuleSeverity | null;
  createdAt: string;
  updatedAt: string;
  isDeleted: boolean;
  deletedAt: string | null;
}

export interface EscalationRuleResource {
  escalationRule: EscalationRule;
  organization?: Organization;
}

export type ApprovalSubject =
  | "RuleDeployment"
  | "CaseResolution"
  | "SuspiciousActivityReport";

export const APPROVAL_SUBJECTS: ApprovalSubject[] = [
  "RuleDeployment",
  "CaseResolution",
  "SuspiciousActivityReport",
];

export type ApprovalStatus = "Pending" | "Approved" | "Rejected" | "Withdrawn";

export const APPROVAL_STATUSES: ApprovalStatus[] = [
  "Pending",
  "Approved",
  "Rejected",
  "Withdrawn",
];

export interface ProposedCaseResolution {
  status: "Approved" | "Rejected";
  disposition: CaseDisposition;
  caseReasonCodeId: string | null;
  note: string | null;
}

export interface Approval {
  approvalId: string;
  organizationId: string;
  subject: ApprovalSubject;
  ruleDeploymentId: string | null;
  caseId: string | null;
  suspiciousActivityReportId: string | null;
  proposedCaseResolution: ProposedCaseResolution | null;
  status: ApprovalStatus;
  reason: string | null;
  requestedByUserId: string;
  reviewedByUserId: string | null;
  reviewedAt: string | null;
  reviewNote: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface ApprovalResource {
  approval: Approval;
  organization?: Organization;
  requestedByUser?: ApplicationUser;
  reviewedByUser?: ApplicationUser;
  ruleDeployment?: RuleDeployment;
  rule?: Rule;
  suspiciousActivityReport?: SuspiciousActivityReport;
}

export interface ApprovalPolicy {
  organizationId: string;
  requireDeploymentApproval: boolean;
  caseDecisionMinimumSeverity: RuleSeverity | null;
  caseDecisionMatcher: RuleBodyDto | null;
  createdAt: string;
  updatedAt: string;
}

export interface ApprovalPolicyBody {
  requireDeploymentApproval: boolean;
  caseDecisionMinimumSeverity: string | null;
  caseDecisionMatcher: RuleBodyDto | null;
}

export type AttachmentScanStatus = "Pending" | "Clean" | "Infected" | "Failed";

export const ATTACHMENT_SCAN_STATUSES: readonly AttachmentScanStatus[] = [
  "Clean",
  "Pending",
  "Failed",
  "Infected",
];

export interface Attachment {
  attachmentId: string;
  caseId: string;
  organizationId: string;
  fileName: string;
  contentType: string;
  byteCount: number;
  checksum: string;
  scanStatus: AttachmentScanStatus | (string & {});
  scannedAt: string | null;
  threatName: string | null;
  uploadedByUserId: string;
  createdAt: string;
  updatedAt: string;
}

export interface AttachmentResource {
  attachment: Attachment;
  uploadedByUser?: ApplicationUser;
}

export interface CaseLink {
  caseLinkId: string;
  organizationId: string;
  caseId: string;
  linkedCaseId: string;
  reason: string | null;
  createdByUserId: string;
  createdAt: string;
  updatedAt: string;
}

export interface CaseLinkResource {
  caseLink: CaseLink;
  linkedCase?: Case;
  createdByUser?: ApplicationUser;
}

export type NotificationChannel = "Email" | "Chat";

export const NOTIFICATION_CHANNELS: readonly NotificationChannel[] = [
  "Email",
  "Chat",
];

export interface NotificationSubscription {
  notificationSubscriptionId: string;
  organizationId: string;
  channel: NotificationChannel | (string & {});
  destination: string;
  minimumSeverity: RuleSeverity | null;
  notifyOnBreach: boolean;
  isEnabled: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface NotificationSubscriptionResource {
  notificationSubscription: NotificationSubscription;
}

export interface NotificationSubscriptionPatch {
  destination?: string;
  minimumSeverity?: RuleSeverity;
  notifyOnBreach?: boolean;
  isEnabled?: boolean;
}

export type SuspiciousActivityReportStatus =
  | "Draft"
  | "PendingApproval"
  | "Approved"
  | "Filed"
  | "Acknowledged"
  | "NotFiled";

export const SUSPICIOUS_ACTIVITY_REPORT_STATUSES: SuspiciousActivityReportStatus[] =
  [
    "Draft",
    "PendingApproval",
    "Approved",
    "Filed",
    "Acknowledged",
    "NotFiled",
  ];

export interface SuspiciousActivityReport {
  suspiciousActivityReportId: string;
  organizationId: string;
  caseId: string;
  friendlyName: string;
  status: SuspiciousActivityReportStatus;
  narrative: string;
  dueAt: string;
  isOverdue: boolean;
  openedByUserId: string;
  filedByUserId: string | null;
  filedAt: string | null;
  filingReference: string | null;
  acknowledgedAt: string | null;
  acknowledgementReference: string | null;
  notFiledReason: string | null;
  decidedByUserId: string | null;
  decidedAt: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface SuspiciousActivityReportResource {
  suspiciousActivityReport: SuspiciousActivityReport;
  requireFilingApproval: boolean;
  organization?: Organization;
  openedByUser?: ApplicationUser;
  filedByUser?: ApplicationUser;
  decidedByUser?: ApplicationUser;
}

export interface SuspiciousActivityReportPolicy {
  organizationId: string;
  reportingEntityId: string | null;
  filingDeadlineDays: number | null;
  effectiveFilingDeadlineDays: number;
  requireFilingApproval: boolean;
  amountPath: string | null;
  currencyPath: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface SuspiciousActivityReportPolicyBody {
  reportingEntityId?: string | null;
  filingDeadlineDays?: number | null;
  requireFilingApproval: boolean;
  amountPath?: string | null;
  currencyPath?: string | null;
}

export type PermissionScopeType =
  | "Organization"
  | "SubOrganization"
  | "RuleSet"
  | "Queue";

export const PERMISSION_SCOPE_TYPES: PermissionScopeType[] = [
  "Organization",
  "SubOrganization",
  "RuleSet",
  "Queue",
];

export interface PermissionScope {
  scopeType: PermissionScopeType;
  subOrganizationId?: string | null;
  ruleSetId?: string | null;
  queueId?: string | null;
}

export interface PermissionGrant {
  name: string;
  scope: PermissionScope;
}

export interface PermissionCatalogueEntry {
  name: string;
  resource: string;
  action: string;
  isGrantable: boolean;
  isHeld: boolean;
}

export interface Role {
  roleId: string;
  organizationId: string | null;
  name: string;
  description: string | null;
  isBuiltIn: boolean;
  permissions: string[];
  createdAt: string;
  updatedAt: string;
}

export interface RoleUsage {
  roleId: string;
  name: string;
  assignmentCount: number;
  userIds: string[];
}

export interface RoleBody {
  name?: string;
  description?: string | null;
  permissions?: string[];
}

export interface RoleAssignment {
  roleAssignmentId: string;
  userId: string;
  roleId: string;
  roleName: string | null;
  scope: PermissionScope;
  assignedByUserId: string;
  createdAt: string;
}

export interface RoleAssignmentBody {
  roleId: string;
  scopeType?: PermissionScopeType;
  subOrganizationId?: string;
  ruleSetId?: string;
  queueId?: string;
}

export interface SubOrganization {
  subOrganizationId: string;
  organizationId: string;
  name: string;
  description: string | null;
  externalReference: string | null;
  isEnabled: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface SubOrganizationUsage {
  subOrganizationId: string;
  name: string;
  userCount: number;
  queueCount: number;
  ruleSetCount: number;
  roleAssignmentCount: number;
}

export interface SubOrganizationBody {
  name?: string;
  description?: string | null;
  externalReference?: string | null;
  clearExternalReference?: boolean;
  isEnabled?: boolean;
}

export interface RoleResource {
  role: Role;
  organization?: Organization;
}

export interface RoleAssignmentResource {
  roleAssignment: RoleAssignment;
  role?: Role;
  user?: ApplicationUser;
  assignedByUser?: ApplicationUser;
}

export interface SubOrganizationResource {
  subOrganization: SubOrganization;
  organization?: Organization;
}

export interface RateLimitOverride {
  rateLimitOverrideId: string;
  organizationId: string;
  pool: RateLimitOverridePool | null;
  permitLimit: number;
  tokensPerPeriod: number;
  replenishmentPeriodSeconds: number;
  reason: string;
  expiresAt: string;
  createdByUserId: string;
  createdAt: string;
  updatedAt: string;
}

export type RateLimitOverridePool = "Read" | "Write" | "Evaluation";

export interface PermissionDeniedProblemDetails extends ProblemDetails {
  type?: string;
  title?: string;
  requiredPermission?: string;
}

export interface PermissionNotGrantableProblemDetails extends ProblemDetails {
  type?: string;
  title?: string;
  permissions?: string[];
}

export interface RoleInUseProblemDetails extends ProblemDetails {
  type?: string;
  title?: string;
  assignmentCount?: number;
  userIds?: string[];
}

export type ServiceComponent =
  | "Api"
  | "RuleEvaluation"
  | "Webhooks"
  | "BackgroundProcessing"
  | "Reporting";

export type ComponentStatus =
  | "Operational"
  | "Degraded"
  | "PartialOutage"
  | "MajorOutage"
  | "UnderMaintenance";

/** `totalProbes: 0` means no data, not perfect uptime. See lib/status. */
export interface ComponentUptimeDay {
  date: string;
  totalProbes: number;
  healthyProbes: number;
  downtimeSeconds: number;
  uptimePercentage: number;
}

export interface ComponentHealth {
  component: ServiceComponent;
  status: ComponentStatus;
  lastProbedAt: string | null;
  latencyMilliseconds: number | null;
  uptimePercentage: number;
  history: ComponentUptimeDay[];
}

export interface ServiceStatus {
  status: ComponentStatus;
  observedAt: string;
  components: ComponentHealth[];
  activeIncidents: Incident[];
  upcomingMaintenance: MaintenanceWindow[];
}

export type IncidentStatus =
  | "Investigating"
  | "Identified"
  | "Monitoring"
  | "Resolved";

export type IncidentImpact = "None" | "Minor" | "Major" | "Critical";

export interface IncidentUpdate {
  incidentUpdateId: string;
  status: IncidentStatus;
  body: string;
  postedAt: string;
}

export interface Incident {
  incidentId: string;
  incidentNumber: string;
  title: string;
  summary: string;
  status: IncidentStatus;
  impact: IncidentImpact;
  components: ServiceComponent[];
  startedAt: string;
  resolvedAt: string | null;
  rootCause: string | null;
  remediation: string | null;
  reviewPublishedAt: string | null;
  updates: IncidentUpdate[];
  createdAt: string;
  updatedAt: string;
}

export type MaintenanceStatus =
  | "Scheduled"
  | "InProgress"
  | "Completed"
  | "Cancelled";

export interface MaintenanceWindow {
  maintenanceWindowId: string;
  title: string;
  description: string;
  status: MaintenanceStatus;
  components: ServiceComponent[];
  scheduledStart: string;
  scheduledEnd: string;
  announcedAt: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface AvailabilityMeasurement {
  organizationId: string;
  periodStart: string;
  periodEnd: string;
  totalRequests: number;
  failedRequests: number;
  uptimePercentage: number;
  meanLatencyMilliseconds: number;
  percentileLatencyMilliseconds: number;
  percentile: number;
  maxLatencyMilliseconds: number;
  availabilityTarget: number | null;
  latencyTargetMilliseconds: number | null;
  metAvailabilityTarget: boolean | null;
  metLatencyTarget: boolean | null;
}

export interface ServiceCreditBand {
  serviceCreditBandId: string;
  belowUptimePercentage: number;
  creditPercentage: number;
}

export interface AvailabilityCommitment {
  availabilityCommitmentId: string;
  organizationId: string;
  availabilityTarget: number;
  latencyTargetMilliseconds: number;
  latencyPercentile: number;
  effectiveFrom: string;
  effectiveTo: string | null;
  creditBands: ServiceCreditBand[];
  createdByUserId: string | null;
  createdAt: string;
  updatedAt: string;
}

export type ServiceCreditCurrency = "Gbp" | "Eur" | "Usd";

export interface ServiceCredit {
  serviceCreditId: string;
  organizationId: string;
  availabilityCommitmentId: string;
  month: number;
  year: number;
  availabilityTarget: number;
  measuredUptimePercentage: number;
  totalRequests: number;
  failedRequests: number;
  creditPercentage: number;
  amount: number;
  /** Not `CurrencyCode`; normalise via lib/currency before formatting. */
  currency: ServiceCreditCurrency;
  invoiceId: string | null;
  creditNoteId: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface StatusSubscription {
  statusSubscriptionId: string;
  message: string;
}

export type InvitationStatus = "Pending" | "Accepted" | "Revoked" | "Expired";

export type InvitableRole = "User" | "Manager" | "Auditor";

export interface Invitation {
  invitationId: string;
  organizationId: string;
  email: string;
  role: InvitableRole | (string & {});
  subOrganizationId: string | null;
  invitedByUserId: string;
  invitedByPlatformOperator: boolean;
  status: InvitationStatus | (string & {});
  expiresAt: string;
  acceptedAt: string | null;
  acceptedByUserId: string | null;
  revokedAt: string | null;
  revokedByUserId: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface IssuedInvitation extends Invitation {
  invitationToken: string;
  acceptUrl: string;
}

export interface InvitationResource {
  invitation: Invitation;
  organization?: Organization;
  invitedByUser?: ApplicationUser;
  acceptedByUser?: ApplicationUser;
}

export interface IssuedInvitationResource {
  invitation: IssuedInvitation;
  organization?: Organization;
  invitedByUser?: ApplicationUser;
}

export interface InvitationPreview {
  email: string;
  organizationName: string;
  role: InvitableRole | (string & {});
  expiresAt: string;
}

export interface AcceptedInvitation {
  userId: string;
  organizationId: string;
  role: InvitableRole | (string & {});
  isCreated: boolean;
}

export interface OffboardedUser {
  userId: string;
  successorUserId: string | null;
  reassignedCaseCount: number;
  queueMembershipCount: number;
  revokedRoleAssignmentCount: number;
  revokedInvitationCount: number;
}

/** The extension is `invitationStatus` because `status` is reserved by RFC 7807 and gets dropped. */
export interface InvitationNotPendingProblemDetails extends ProblemDetails {
  type?: string;
  title?: string;
  invitationStatus?: string;
}

export interface InvitationAlreadyMemberProblemDetails extends ProblemDetails {
  type?: string;
  title?: string;
  sameOrganization?: boolean;
}

export type RevenueCurrency = CurrencyCode | CollectedCurrency;

export interface RecurringRevenueMonth {
  month: number;
  year: number;
  currency: RevenueCurrency;
  openingRevenue: number;
  newRevenue: number;
  reactivationRevenue: number;
  expansionRevenue: number;
  contractionRevenue: number;
  churnedRevenue: number;
  involuntaryChurnedRevenue: number;
  closingRevenue: number;
  openingCustomers: number;
  newCustomers: number;
  reactivatedCustomers: number;
  churnedCustomers: number;
  involuntarilyChurnedCustomers: number;
  closingCustomers: number;
}

export interface RevenueConcentrationCustomer {
  rank: number;
  organizationId: string;
  organizationName: string;
  revenue: number;
  share: number;
}

export interface RevenueConcentration {
  month: number;
  year: number;
  currency: RevenueCurrency;
  totalRevenue: number;
  customerCount: number;
  largestCustomerShare: number | null;
  topFiveShare: number | null;
  topTenShare: number | null;
  concentrationIndex: number | null;
  customers: RevenueConcentrationCustomer[];
}

export interface CustomerMarginLine {
  billableResource: BillableResource;
  count: number;
  unitCost: number | null;
  cost: number;
}

export interface CustomerMargin {
  organizationId: string;
  organizationName: string;
  month: number;
  year: number;
  currency: RevenueCurrency;
  revenue: number;
  cost: number;
  margin: number;
  marginRate: number | null;
  costsComplete: boolean;
  lines: CustomerMarginLine[];
}

export interface RecoveryByStage {
  stage: DunningStage;
  resolved: number;
}

export interface RecoveryStatistic {
  periodStart: string;
  periodEnd: string;
  currency: RevenueCurrency;
  laddersOpened: number;
  laddersResolved: number;
  recoveryRate: number | null;
  recoveredRevenue: number;
  outstandingRevenue: number;
  restrictionsProposed: number;
  meanDaysToRecover: number | null;
  resolvedByStage: RecoveryByStage[];
  involuntarilyChurnedCustomers: number;
  involuntaryChurnRate: number | null;
}

export type HealthSignalKind =
  | "UsageDecline"
  | "ErrorRateRise"
  | "Silence"
  | "Arrears"
  | "NoBaseline";

export interface HealthSignal {
  kind: HealthSignalKind;
  penalty: number;
  detail: string;
}

export type HealthBand = "Healthy" | "Watch" | "AtRisk";

export interface CustomerHealth {
  organizationId: string;
  organizationName: string;
  healthScore: number;
  healthBand: HealthBand;
  periodStart: string;
  periodEnd: string;
  recentUsage: number;
  previousUsage: number;
  usageTrend: number | null;
  recentErrorRate: number;
  previousErrorRate: number;
  daysSinceLastUsage: number | null;
  arrearsStage: DunningStage | null;
  signals: HealthSignal[];
}

export interface ServiceCostRate {
  serviceCostRateId: string;
  billableResource: BillableResource;
  unitCost: number;
  currency: RevenueCurrency;
  effectiveFrom: string;
  createdByUserId: string | null;
  createdAt: string;
}
