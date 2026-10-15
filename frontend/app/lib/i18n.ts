// The import attribute is required by Node's ESM loader, which is what the
// `node --test` suites run under, and is understood by the bundler too. Without
// it the whole of `lib/format`'s test suite fails to load rather than failing an
// assertion, which is a considerably worse signal.
import en from "@/messages/en.json" with { type: "json" };

export const SUPPORTED_LANGUAGES = ["en", "fr", "de", "es", "nl"] as const;

export type SupportedLanguage = (typeof SUPPORTED_LANGUAGES)[number];

export type MessageCatalogue = Record<string, string>;

export const ENGLISH: MessageCatalogue = en as MessageCatalogue;

export const isSupportedLanguage = (
  language: string
): language is SupportedLanguage =>
  (SUPPORTED_LANGUAGES as readonly string[]).includes(language);

export const interpolate = (
  message: string,
  args: readonly (string | number)[]
): string =>
  message.replace(/\{(\d+)\}/g, (placeholder, index: string) => {
    const argument = args[Number(index)];
    return argument === undefined ? placeholder : String(argument);
  });

export const translate = (
  catalogue: MessageCatalogue,
  key: string,
  args: readonly (string | number)[] = [],
  fallback: MessageCatalogue = ENGLISH
): string => {
  const message = catalogue[key] ?? fallback[key];

  if (message === undefined) return key;

  return args.length > 0 ? interpolate(message, args) : message;
};

export const loadCatalogue = async (
  language: string
): Promise<MessageCatalogue> => {
  if (!isSupportedLanguage(language) || language === "en") return ENGLISH;

  try {
    // Not named `module`: Next forbids assigning that identifier, because a
    // bundle that shadows it breaks the CommonJS interop shim.
    const catalogue = await import(`@/messages/${language}.json`);
    return catalogue.default as MessageCatalogue;
  } catch {
    return ENGLISH;
  }
};

let readerCatalogue: MessageCatalogue = ENGLISH;

export const publishReaderCatalogue = (catalogue: MessageCatalogue): void => {
  readerCatalogue = catalogue;
};

export const translateForReader = (
  key: string,
  ...args: readonly (string | number)[]
): string => translate(readerCatalogue, key, args);
