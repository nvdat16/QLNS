import type { PageMetadata } from "../api/apiClient";

interface Props {
  page: PageMetadata;
  itemLabel: string;
  onPageChange: (page: number) => void;
}

export function Pagination({ page, itemLabel, onPageChange }: Props) {
  const from = page.totalItems === 0 ? 0 : (page.page - 1) * page.pageSize + 1;
  const to = Math.min(page.page * page.pageSize, page.totalItems);

  return (
    <div className="flex flex-col gap-3 border-t border-slate-100 px-5 py-3 text-sm text-slate-500 sm:flex-row sm:items-center sm:justify-between">
      <p>
        Hiển thị {from}–{to} trên {page.totalItems} {itemLabel}
      </p>
      <div className="flex items-center gap-2">
        <button
          type="button"
          disabled={page.page <= 1}
          onClick={() => onPageChange(page.page - 1)}
          className="rounded-lg border border-slate-200 px-3 py-1.5 font-medium text-slate-600 hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-40"
        >
          Trước
        </button>
        <span className="px-2 font-semibold text-slate-700">
          {page.page} / {Math.max(page.totalPages, 1)}
        </span>
        <button
          type="button"
          disabled={page.page >= page.totalPages}
          onClick={() => onPageChange(page.page + 1)}
          className="rounded-lg border border-slate-200 px-3 py-1.5 font-medium text-slate-600 hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-40"
        >
          Tiếp
        </button>
      </div>
    </div>
  );
}
