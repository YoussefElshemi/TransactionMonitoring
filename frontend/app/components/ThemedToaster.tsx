"use client";

import { Toaster } from "sonner";

import { useTheme } from "@/context/ThemeContext";
import { useTranslation } from "@/context/LocalizationContext";

export function ThemedToaster() {
  const { theme } = useTheme();
  const t = useTranslation();

  return (
    <Toaster
      position="top-center"
      richColors
      theme={theme}
      containerAriaLabel={t("a11y.toasts")}
      toastOptions={{
        classNames: {
          toast: "has-[[data-description]]:flex-wrap has-[[data-description]]:gap-y-3",
          // `min-w-0` unconditionally: a flex child defaults to `min-width:auto`,
          // so without it a long unbroken string — a rule name, a reference —
          // pushes the toast wider than its own width rather than wrapping.
          content: "min-w-0 has-[[data-description]]:basis-full",
          actionButton:
            "!bg-primary !text-primary-foreground hover:!bg-primary/90 !rounded-md !font-mono !text-xs !font-medium !px-2.5 !h-7 transition-colors",
          cancelButton:
            "!bg-transparent !text-muted-foreground hover:!text-foreground !rounded-md !font-mono !text-xs !px-2.5 !h-7 !border !border-input transition-colors",
        },
      }}
    />
  );
}
