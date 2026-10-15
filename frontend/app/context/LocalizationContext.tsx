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

import { useAuth } from "@/context/AuthContext";
import { publishReaderPreferences } from "@/lib/format";
import {
  ENGLISH,
  MessageCatalogue,
  loadCatalogue,
  publishReaderCatalogue,
  translate,
} from "@/lib/i18n";
import {
  DisplayPreferences,
  FALLBACK_LANGUAGE,
  FALLBACK_LOCALE,
  FALLBACK_TIME_ZONE,
  fetchPreferences,
} from "@/lib/localization";

interface LocalizationContextType {
  locale: string;
  timeZone: string;
  language: string;
  messages: MessageCatalogue;
  stated: { locale: string | null; timeZone: string | null };
  ready: boolean;
  refresh: () => Promise<void>;
}

const LocalizationContext = createContext<LocalizationContextType | undefined>(
  undefined
);

export const LocalizationProvider = ({ children }: { children: ReactNode }) => {
  const { user, authResolved } = useAuth();
  const [preferences, setPreferences] = useState<DisplayPreferences | null>(null);
  const [messages, setMessages] = useState<MessageCatalogue>(ENGLISH);
  const [ready, setReady] = useState(false);

  const load = useCallback(async (signal?: AbortSignal) => {
    try {
      setPreferences(await fetchPreferences(signal));
    } catch {
      setPreferences(null);
    } finally {
      setReady(true);
    }
  }, []);

  useEffect(() => {
    if (!authResolved) return;
    if (!user) {
      setPreferences(null);
      setReady(true);
      return;
    }

    const controller = new AbortController();
    void load(controller.signal);
    return () => controller.abort();
  }, [authResolved, user?.userId, load]);

  useEffect(() => {
    const language = preferences?.resolvedLanguage ?? FALLBACK_LANGUAGE;

    let current = true;
    void loadCatalogue(language).then((catalogue) => {
      // Guarded: a reader can change language while the first load is still in
      // flight, and the slower response must not win.
      if (!current) return;

      setMessages(catalogue);
      publishReaderCatalogue(catalogue);
    });

    return () => {
      current = false;
    };
  }, [preferences?.resolvedLanguage]);

  useEffect(() => {
    document.documentElement.lang =
      preferences?.resolvedLanguage ?? FALLBACK_LANGUAGE;
  }, [preferences?.resolvedLanguage]);

  useEffect(() => {
    publishReaderPreferences(
      preferences?.resolvedLocale ?? FALLBACK_LOCALE,
      preferences?.resolvedTimeZone ?? FALLBACK_TIME_ZONE
    );
  }, [preferences]);

  const value = useMemo<LocalizationContextType>(
    () => ({
      locale: preferences?.resolvedLocale ?? FALLBACK_LOCALE,
      timeZone: preferences?.resolvedTimeZone ?? FALLBACK_TIME_ZONE,
      language: preferences?.resolvedLanguage ?? FALLBACK_LANGUAGE,
      messages,
      stated: {
        locale: preferences?.locale ?? null,
        timeZone: preferences?.timeZone ?? null,
      },
      ready,
      refresh: () => load(),
    }),
    [preferences, messages, ready, load]
  );

  return (
    <LocalizationContext.Provider value={value}>
      {children}
    </LocalizationContext.Provider>
  );
};

export const useLocalization = (): LocalizationContextType => {
  const context = useContext(LocalizationContext);

  return (
    context ?? {
      locale: FALLBACK_LOCALE,
      timeZone: FALLBACK_TIME_ZONE,
      language: FALLBACK_LANGUAGE,
      messages: ENGLISH,
      stated: { locale: null, timeZone: null },
      ready: true,
      refresh: async () => {},
    }
  );
};

export const useTranslation = (): ((
  key: string,
  ...args: (string | number)[]
) => string) => {
  const { messages } = useLocalization();

  return useCallback(
    (key: string, ...args: (string | number)[]) => translate(messages, key, args),
    [messages]
  );
};
