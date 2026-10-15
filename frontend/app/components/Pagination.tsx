"use client";

import { PaginationProps } from "@/types";
import { Button } from "@/components/ui/button";
import { ChevronLeft, ChevronRight } from "lucide-react";

import { useTranslation } from "@/context/LocalizationContext";
import { useFormat } from "@/lib/useFormat";

const Pagination = ({
  handlePrevPage,
  pageNumber,
  totalPages,
  handleNextPage,
}: PaginationProps) => {
  const t = useTranslation();
  const format = useFormat();

  return (
    <nav
      aria-label={t("a11y.pagination")}
      className="mt-4 flex items-center justify-between"
    >
      <Button
        variant="outline"
        size="sm"
        onClick={handlePrevPage}
        disabled={pageNumber === 1}
        className="gap-1.5"
      >
        <ChevronLeft aria-hidden className="size-4" />
        {t("a11y.page.previous")}
      </Button>
      <span
        aria-live="polite"
        className="font-mono text-xs uppercase tracking-widest text-slate-400"
      >
        {t(
          "a11y.page.position",
          format.count(pageNumber),
          format.count(totalPages)
        )}
      </span>
      <Button
        variant="outline"
        size="sm"
        onClick={handleNextPage}
        disabled={pageNumber === totalPages}
        className="gap-1.5"
      >
        {t("a11y.page.next")}
        <ChevronRight aria-hidden className="size-4" />
      </Button>
    </nav>
  );
};

export default Pagination;
