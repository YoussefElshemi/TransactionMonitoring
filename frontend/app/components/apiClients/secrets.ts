import { ApiClientSecret, CreatedApiClientSecret } from "@/types";

export type SecretState = "Active" | "Expired" | "Revoked";

export const secretState = (
  secret: ApiClientSecret,
  now: number = Date.now()
): SecretState => {
  if (secret.revokedAt !== null) return "Revoked";
  if (secret.expiresAt !== null && new Date(secret.expiresAt).getTime() <= now) {
    return "Expired";
  }
  return "Active";
};

export const isUsable = (secret: ApiClientSecret): boolean =>
  secretState(secret) === "Active";

export const usableCount = (secrets: ApiClientSecret[]): number =>
  secrets.filter(isUsable).length;

export const withoutPlaintext = (
  created: CreatedApiClientSecret
): ApiClientSecret => ({
  apiClientSecretId: created.apiClientSecretId,
  apiClientId: created.apiClientId,
  secretPrefix: created.secretPrefix,
  expiresAt: created.expiresAt,
  revokedAt: created.revokedAt,
  lastUsedAt: created.lastUsedAt,
  createdAt: created.createdAt,
  updatedAt: created.updatedAt,
});
