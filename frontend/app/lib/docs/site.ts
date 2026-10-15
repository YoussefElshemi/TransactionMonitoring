export const DOCS_API_BASE = (
  process.env.NEXT_PUBLIC_API_BASE ?? "http://localhost:5253"
).replace(/\/+$/, "");

export const DOCS_API_VERSION = "2026-10-14";

export interface DocsNavItem {
  href: string;
  label: string;
}

export interface DocsNavSection {
  title: string;
  items: DocsNavItem[];
}

export const DOCS_NAV: DocsNavSection[] = [
  {
    title: "Get started",
    items: [
      { href: "/docs", label: "Overview" },
      { href: "/docs/authentication", label: "Authentication" },
      { href: "/docs/screening", label: "Screening a transaction" },
    ],
  },
  {
    title: "Concepts",
    items: [
      { href: "/docs/conventions", label: "Request conventions" },
      { href: "/docs/errors", label: "Errors" },
      { href: "/docs/webhooks", label: "Webhooks" },
    ],
  },
  {
    title: "Reference",
    items: [{ href: "/docs/reference", label: "Endpoint reference" }],
  },
];

const DOCS_PAGES = DOCS_NAV.flatMap((section) => section.items);

export const docsNeighbours = (href: string) => {
  const index = DOCS_PAGES.findIndex((page) => page.href === href);

  return {
    previous: index > 0 ? DOCS_PAGES[index - 1] : null,
    next: index >= 0 && index < DOCS_PAGES.length - 1 ? DOCS_PAGES[index + 1] : null,
  };
};
