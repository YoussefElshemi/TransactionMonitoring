export type EnvironmentMode = "live" | "test";

export const MODE_STORAGE_KEY = "transaction-monitoring:environment";
export const MODE_HEADER = "X-Mode";

let activeMode: EnvironmentMode = "live";
const listeners = new Set<(mode: EnvironmentMode) => void>();
const cacheInvalidators = new Set<() => void>();

const isEnvironmentMode = (value: string | null): value is EnvironmentMode =>
  value === "live" || value === "test";

export const currentEnvironmentMode = (): EnvironmentMode => activeMode;

export const storedEnvironmentMode = (): EnvironmentMode => {
  if (typeof window === "undefined") return "live";

  try {
    const stored = window.localStorage.getItem(MODE_STORAGE_KEY);
    return isEnvironmentMode(stored) ? stored : "live";
  } catch {
    return "live";
  }
};

export const setEnvironmentMode = (
  next: EnvironmentMode,
  { persist = true }: { persist?: boolean } = {}
): void => {
  if (activeMode === next) return;

  activeMode = next;
  invalidateModeScopedCaches();

  if (persist && typeof window !== "undefined") {
    try {
      window.localStorage.setItem(MODE_STORAGE_KEY, next);
    } catch {
    }
  }

  listeners.forEach((listener) => listener(next));
};

export const subscribeToEnvironmentMode = (
  listener: (mode: EnvironmentMode) => void
): (() => void) => {
  listeners.add(listener);
  return () => listeners.delete(listener);
};

export const registerModeScopedCache = (
  invalidate: () => void
): (() => void) => {
  cacheInvalidators.add(invalidate);
  return () => cacheInvalidators.delete(invalidate);
};

export const invalidateModeScopedCaches = (): void => {
  cacheInvalidators.forEach((invalidate) => invalidate());
};

export const modeRequestHeader = (): EnvironmentMode => activeMode;

export const effectiveModeMatches = (
  effective: string | null,
  requested: EnvironmentMode
): boolean => effective === null || effective.toLowerCase() === requested;

export const isTestCredential = (value: string | null | undefined): boolean =>
  Boolean(
    value &&
      (value.startsWith("tmc_test_") || value.startsWith("tmsec_test_"))
  );
