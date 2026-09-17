export type KpiTone = "blue" | "emerald" | "amber" | "slate";

const toneText: Record<KpiTone, string> = {
  blue: "text-blue-600",
  emerald: "text-emerald-600",
  amber: "text-amber-600",
  slate: "text-slate-500",
};

interface Props {
  title: string;
  value: string;
  subtitle?: string;
  tone?: KpiTone;
  icon?: string;
}

export function KpiCard({ title, value, subtitle, tone = "slate", icon }: Props) {
  return (
    <div className="hrm-card rounded-2xl border border-[#edf0f4] bg-white p-5 shadow-[0_8px_28px_rgba(28,39,60,0.055)]">
      <div className="flex items-start justify-between">
        <p className="text-sm font-medium text-slate-500">{title}</p>
        {icon && <span className={`material-symbols-outlined icon-md ${toneText[tone]}`}>{icon}</span>}
      </div>
      <p className="mt-2 text-2xl font-bold text-slate-900">{value}</p>
      {subtitle && <p className={`mt-1 text-xs font-medium ${toneText[tone]}`}>{subtitle}</p>}
    </div>
  );
}
