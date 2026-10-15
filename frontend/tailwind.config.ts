import type { Config } from "tailwindcss";

export default {
  darkMode: ["class"],
  content: [
    "./app/pages/**/*.{js,ts,jsx,tsx,mdx}",
    "./app/components/**/*.{js,ts,jsx,tsx,mdx}",
    "./app/**/*.{js,ts,jsx,tsx,mdx}",
  ],
  theme: {
    extend: {
      fontFamily: {
        sans: ["var(--font-geist-sans)", "system-ui", "sans-serif"],
        mono: ["var(--font-geist-mono)", "ui-monospace", "monospace"],
      },
      colors: {
        brand: {
          50: "hsl(var(--brand-50) / <alpha-value>)",
          100: "hsl(var(--brand-100) / <alpha-value>)",
          200: "hsl(var(--brand-200) / <alpha-value>)",
          300: "hsl(var(--brand-300) / <alpha-value>)",
          400: "hsl(var(--brand-400) / <alpha-value>)",
          500: "hsl(var(--brand-500) / <alpha-value>)",
          600: "hsl(var(--brand-600) / <alpha-value>)",
          700: "hsl(var(--brand-700) / <alpha-value>)",
          800: "hsl(var(--brand-800) / <alpha-value>)",
          900: "hsl(var(--brand-900) / <alpha-value>)",
        },
        settled: {
          50: "hsl(var(--settled-50) / <alpha-value>)",
          100: "hsl(var(--settled-100) / <alpha-value>)",
          200: "hsl(var(--settled-200) / <alpha-value>)",
          500: "hsl(var(--settled-500) / <alpha-value>)",
          700: "hsl(var(--settled-700) / <alpha-value>)",
        },
        inflight: {
          50: "hsl(var(--inflight-50) / <alpha-value>)",
          100: "hsl(var(--inflight-100) / <alpha-value>)",
          200: "hsl(var(--inflight-200) / <alpha-value>)",
          500: "hsl(var(--inflight-500) / <alpha-value>)",
          700: "hsl(var(--inflight-700) / <alpha-value>)",
        },
        attention: {
          50: "hsl(var(--attention-50) / <alpha-value>)",
          100: "hsl(var(--attention-100) / <alpha-value>)",
          200: "hsl(var(--attention-200) / <alpha-value>)",
          500: "hsl(var(--attention-500) / <alpha-value>)",
          700: "hsl(var(--attention-700) / <alpha-value>)",
        },
        elevated: {
          50: "hsl(var(--elevated-50) / <alpha-value>)",
          100: "hsl(var(--elevated-100) / <alpha-value>)",
          200: "hsl(var(--elevated-200) / <alpha-value>)",
          500: "hsl(var(--elevated-500) / <alpha-value>)",
          700: "hsl(var(--elevated-700) / <alpha-value>)",
        },
        critical: {
          50: "hsl(var(--critical-50) / <alpha-value>)",
          100: "hsl(var(--critical-100) / <alpha-value>)",
          200: "hsl(var(--critical-200) / <alpha-value>)",
          500: "hsl(var(--critical-500) / <alpha-value>)",
          700: "hsl(var(--critical-700) / <alpha-value>)",
          solid: "hsl(var(--critical-solid) / <alpha-value>)",
          "solid-foreground": "hsl(var(--critical-solid-foreground) / <alpha-value>)",
        },
        // Overrides Tailwind's built-in slate with the same light values, so
        // the ~760 existing `slate-*` classes become theme-aware for free.
        slate: {
          50: "hsl(var(--slate-50) / <alpha-value>)",
          100: "hsl(var(--slate-100) / <alpha-value>)",
          200: "hsl(var(--slate-200) / <alpha-value>)",
          300: "hsl(var(--slate-300) / <alpha-value>)",
          400: "hsl(var(--slate-400) / <alpha-value>)",
          500: "hsl(var(--slate-500) / <alpha-value>)",
          600: "hsl(var(--slate-600) / <alpha-value>)",
          700: "hsl(var(--slate-700) / <alpha-value>)",
          800: "hsl(var(--slate-800) / <alpha-value>)",
          900: "hsl(var(--slate-900) / <alpha-value>)",
          950: "hsl(var(--slate-950) / <alpha-value>)",
        },
        console: {
          DEFAULT: "hsl(var(--console) / <alpha-value>)",
          border: "hsl(var(--console-border) / <alpha-value>)",
          200: "hsl(var(--console-200) / <alpha-value>)",
          300: "hsl(var(--console-300) / <alpha-value>)",
          400: "hsl(var(--console-400) / <alpha-value>)",
          500: "hsl(var(--console-500) / <alpha-value>)",
          600: "hsl(var(--console-600) / <alpha-value>)",
          700: "hsl(var(--console-700) / <alpha-value>)",
        },
        background: "hsl(var(--background) / <alpha-value>)",
        foreground: "hsl(var(--foreground) / <alpha-value>)",
        card: {
          DEFAULT: "hsl(var(--card) / <alpha-value>)",
          foreground: "hsl(var(--card-foreground) / <alpha-value>)",
        },
        popover: {
          DEFAULT: "hsl(var(--popover) / <alpha-value>)",
          foreground: "hsl(var(--popover-foreground) / <alpha-value>)",
        },
        primary: {
          DEFAULT: "hsl(var(--primary) / <alpha-value>)",
          foreground: "hsl(var(--primary-foreground) / <alpha-value>)",
        },
        secondary: {
          DEFAULT: "hsl(var(--secondary) / <alpha-value>)",
          foreground: "hsl(var(--secondary-foreground) / <alpha-value>)",
        },
        muted: {
          DEFAULT: "hsl(var(--muted) / <alpha-value>)",
          foreground: "hsl(var(--muted-foreground) / <alpha-value>)",
        },
        accent: {
          DEFAULT: "hsl(var(--accent) / <alpha-value>)",
          foreground: "hsl(var(--accent-foreground) / <alpha-value>)",
        },
        destructive: {
          DEFAULT: "hsl(var(--destructive) / <alpha-value>)",
          foreground: "hsl(var(--destructive-foreground) / <alpha-value>)",
        },
        border: "hsl(var(--border) / <alpha-value>)",
        input: "hsl(var(--input) / <alpha-value>)",
        ring: "hsl(var(--ring) / <alpha-value>)",
      },
      borderRadius: {
        lg: "var(--radius)",
        md: "calc(var(--radius) - 2px)",
        sm: "calc(var(--radius) - 4px)",
      },
      keyframes: {
        indeterminate: {
          "0%": { transform: "translateX(-100%)" },
          "100%": { transform: "translateX(300%)" },
        },
      },
      animation: {
        indeterminate: "indeterminate 1.4s ease-in-out infinite",
      },
    },
  },
  plugins: [],
} satisfies Config;
