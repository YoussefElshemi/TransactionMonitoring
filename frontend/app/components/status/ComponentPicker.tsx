import type { ServiceComponent } from "@/types";
import { COMPONENT_DESCRIPTIONS, SERVICE_COMPONENTS, componentLabel } from "@/lib/status";
import { cn } from "@/lib/utils";

export function ComponentPicker({
  selected,
  onChange,
  disabled = false,
  className,
}: {
  selected: readonly ServiceComponent[];
  onChange: (components: ServiceComponent[]) => void;
  disabled?: boolean;
  className?: string;
}) {
  const toggle = (component: ServiceComponent) => {
    onChange(
      selected.includes(component)
        ? selected.filter((existing) => existing !== component)
        : [...selected, component]
    );
  };

  return (
    <div className={cn("flex flex-wrap gap-2", className)}>
      {SERVICE_COMPONENTS.map((component) => {
        const active = selected.includes(component);

        return (
          <button
            key={component}
            type="button"
            disabled={disabled}
            aria-pressed={active}
            title={COMPONENT_DESCRIPTIONS[component]}
            onClick={() => toggle(component)}
            className={cn(
              "rounded-md border px-2.5 py-1 font-mono text-[11px] transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring disabled:cursor-not-allowed disabled:opacity-50",
              active
                ? "border-brand-800 bg-brand-800 text-primary-foreground"
                : "border-slate-200 bg-transparent text-slate-500 hover:bg-accent hover:text-accent-foreground"
            )}
          >
            {componentLabel(component)}
          </button>
        );
      })}
    </div>
  );
}
