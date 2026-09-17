import type { ReactNode } from "react";

interface Props {
  breadcrumb: string;
  title: string;
  actions?: ReactNode;
}

export function PageHeader({ breadcrumb, title, actions }: Props) {
  return (
    <div className="mb-6 flex flex-wrap items-end justify-between gap-4">
      <div>
        <p className="text-xs font-medium text-slate-400">{breadcrumb}</p>
        <h1 className="mt-1 text-xl font-bold text-slate-900">{title}</h1>
      </div>
      {actions && <div className="flex items-center gap-3">{actions}</div>}
    </div>
  );
}
