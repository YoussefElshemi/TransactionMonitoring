export function Mark({ className = "" }: { className?: string }) {
  return (
    <svg
      viewBox="0 0 16 16"
      fill="none"
      aria-hidden="true"
      className={className}
    >
      <path
        d="M3.5 2v9.5a1 1 0 0 0 1 1h8M3.5 6.5h6"
        stroke="currentColor"
        strokeWidth="1.5"
        strokeLinecap="round"
      />
      <circle cx="12.5" cy="6.5" r="1.75" fill="currentColor" />
      <circle cx="13" cy="12.5" r="1.75" fill="currentColor" />
    </svg>
  );
}
