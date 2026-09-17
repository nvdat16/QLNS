interface Props {
  icon: string;
  label: string;
  onClick?: () => void;
  tone?: "default" | "brand";
  disabled?: boolean;
}

export function IconButton({ icon, label, onClick, tone = "default", disabled }: Props) {
  const toneClass =
    tone === "brand"
      ? "border-brand-200 text-brand-600 hover:bg-brand-50"
      : "border-slate-200 text-slate-500 hover:bg-slate-50";

  return (
    <button
      type="button"
      title={label}
      aria-label={label}
      disabled={disabled}
      onClick={(event) => {
        event.stopPropagation();
        onClick?.();
      }}
      className={`inline-flex h-8 w-8 items-center justify-center rounded-lg border bg-white disabled:cursor-not-allowed disabled:opacity-40 ${toneClass}`}
    >
      <span className="material-symbols-outlined icon-sm">{icon}</span>
    </button>
  );
}
