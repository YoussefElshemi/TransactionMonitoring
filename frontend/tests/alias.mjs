import { registerHooks } from "node:module";
import { existsSync } from "node:fs";

const APP = new URL("../app/", import.meta.url);

/** TypeScript specifiers carry no extension; ESM requires one. */
const EXTENSIONS = ["", ".ts", ".tsx", "/index.ts"];

registerHooks({
  resolve(specifier, context, nextResolve) {
    if (!specifier.startsWith("@/")) return nextResolve(specifier, context);

    const base = new URL(specifier.slice(2), APP);

    for (const extension of EXTENSIONS) {
      const candidate = new URL(base.href + extension);
      if (existsSync(candidate)) {
        // No `format`: Node picks TypeScript off the extension and strips the types
        // itself, exactly as it does for a relative import in these suites.
        return { url: candidate.href, shortCircuit: true };
      }
    }

    return nextResolve(specifier, context);
  },
});
