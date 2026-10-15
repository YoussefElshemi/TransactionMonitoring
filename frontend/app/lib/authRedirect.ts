export const loginHref = (redirectUri: string) =>
  `/auth/login?redirect_uri=${encodeURIComponent(redirectUri)}`;

export const currentLocation = () =>
  `${window.location.pathname}${window.location.search}`;

export const safeRedirectUri = (
  value: string | null | undefined,
  fallback = "/"
): string => {
  if (!value || !value.startsWith("/")) return fallback;
  // `//host` and `/\host` are protocol-relative URLs, not local paths.
  if (value.startsWith("//") || value.startsWith("/\\")) return fallback;
  return value;
};
