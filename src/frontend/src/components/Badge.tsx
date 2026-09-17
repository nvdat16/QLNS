import type { ReactNode } from "react";

export type BadgeTone = "slate" | "blue" | "amber" | "purple" | "rose" | "emerald";

const toneClasses: Record<BadgeTone, string> = {
  slate: "bg-slate-100 text-slate-700 border-slate-200",
  blue: "bg-blue-100 text-blue-700 border-blue-200",
  amber: "bg-amber-100 text-amber-800 border-amber-200",
  purple: "bg-purple-100 text-purple-800 border-purple-200",
  rose: "bg-rose-100 text-rose-800 border-rose-200",
  emerald: "bg-emerald-100 text-emerald-800 border-emerald-200",
};

const dotClasses: Record<BadgeTone, string> = {
  slate: "bg-slate-400",
  blue: "bg-blue-500",
  amber: "bg-amber-500",
  purple: "bg-purple-500",
  rose: "bg-rose-500",
  emerald: "bg-emerald-500",
};

interface Props {
  tone: BadgeTone;
  children: ReactNode;
  dot?: boolean;
}

export function Badge({ tone, children, dot }: Props) {
  return (
    <span
      className={`inline-flex items-center gap-1.5 whitespace-nowrap rounded-full border px-2.5 py-1 text-xs font-semibold ${toneClasses[tone]}`}
    >
      {dot && <span className={`h-1.5 w-1.5 rounded-full ${dotClasses[tone]}`} />}
      {children}
    </span>
  );
}
