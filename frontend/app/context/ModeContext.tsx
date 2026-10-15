"use client";

import {
  createContext,
  ReactNode,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
} from "react";
import { useRouter } from "next/navigation";

import {
  EnvironmentMode,
  MODE_STORAGE_KEY,
  currentEnvironmentMode,
  invalidateModeScopedCaches,
  setEnvironmentMode,
  storedEnvironmentMode,
  subscribeToEnvironmentMode,
} from "@/lib/mode";
import { resetRateLimitState } from "@/lib/rateLimit";

interface ModeContextType {
  mode: EnvironmentMode;
  ready: boolean;
  environmentEpoch: number;
  setMode: (mode: EnvironmentMode) => void;
  refreshEnvironment: () => void;
}

const ModeContext = createContext<ModeContextType | undefined>(undefined);

export const ModeProvider = ({ children }: { children: ReactNode }) => {
  const router = useRouter();
  const [mode, setModeState] = useState<EnvironmentMode>(
    currentEnvironmentMode
  );
  const [ready, setReady] = useState(false);
  const [environmentEpoch, setEnvironmentEpoch] = useState(0);

  const refreshEnvironment = useCallback(() => {
    invalidateModeScopedCaches();
    resetRateLimitState();
    setEnvironmentEpoch((epoch) => epoch + 1);
    router.refresh();
  }, [router]);

  useEffect(() => {
    const initial = storedEnvironmentMode();
    setEnvironmentMode(initial, { persist: false });
    setModeState(initial);
    setReady(true);

    const unsubscribe = subscribeToEnvironmentMode((next) => {
      setModeState(next);
      refreshEnvironment();
    });

    const onStorage = (event: StorageEvent) => {
      if (event.key !== MODE_STORAGE_KEY) return;
      const next = storedEnvironmentMode();
      setEnvironmentMode(next, { persist: false });
    };

    window.addEventListener("storage", onStorage);
    return () => {
      unsubscribe();
      window.removeEventListener("storage", onStorage);
    };
  }, [refreshEnvironment]);

  const setMode = useCallback((next: EnvironmentMode) => {
    setEnvironmentMode(next);
  }, []);

  const value = useMemo<ModeContextType>(
    () => ({
      mode,
      ready,
      environmentEpoch,
      setMode,
      refreshEnvironment,
    }),
    [environmentEpoch, mode, ready, refreshEnvironment, setMode]
  );

  return <ModeContext.Provider value={value}>{children}</ModeContext.Provider>;
};

export const useEnvironment = (): ModeContextType => {
  const context = useContext(ModeContext);
  if (!context) {
    throw new Error("useEnvironment must be used within a ModeProvider");
  }
  return context;
};
