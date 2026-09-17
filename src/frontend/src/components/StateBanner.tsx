interface Props {
  loading?: boolean;
  error?: string;
  onRetry?: () => void;
  loadingLabel?: string;
}

export function StateBanner({ loading, error, onRetry, loadingLabel = "Đang tải dữ liệu…" }: Props) {
  if (error) {
    return (
      <div role="alert" className="flex items-center justify-between gap-3 rounded-xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm text-rose-700">
        <span>{error}</span>
        {onRetry && (
          <button type="button" onClick={onRetry} className="shrink-0 font-semibold underline">
            Thử lại
          </button>
        )}
      </div>
    );
  }

  if (loading) {
    return (
      <p role="status" className="rounded-xl border border-slate-200 bg-white px-4 py-3 text-sm text-slate-500">
        {loadingLabel}
      </p>
    );
  }

  return null;
}
