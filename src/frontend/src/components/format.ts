export function formatCurrency(amount: number, currency = "VND"): string {
  return new Intl.NumberFormat("vi-VN", { style: "currency", currency, maximumFractionDigits: 0 }).format(amount);
}

export function formatDate(value?: string): string {
  if (!value) return "—";
  return new Date(value).toLocaleDateString("vi-VN");
}
