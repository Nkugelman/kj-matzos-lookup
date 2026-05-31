export function digitsOnly(value: string): string {
  return value.replace(/\D/g, "")
}

export function formatPhoneInput(value: string): string {
  const d = digitsOnly(value).slice(0, 10)
  if (d.length <= 3) return d
  if (d.length <= 6) return `(${d.slice(0, 3)}) ${d.slice(3)}`
  return `(${d.slice(0, 3)}) ${d.slice(3, 6)}-${d.slice(6)}`
}

export function isValidPhone(value: string): boolean {
  return digitsOnly(value).length >= 10
}

export function formatCurrency(amount: number): string {
  return new Intl.NumberFormat("en-US", { style: "currency", currency: "USD" }).format(amount)
}

export function formatOrderDate(value?: string): string {
  if (!value) return "—"
  const d = new Date(value)
  if (Number.isNaN(d.getTime())) return value
  return d.toLocaleDateString("en-US", { month: "short", day: "numeric", year: "numeric" })
}

export function buildYearOptions(count = 8): number[] {
  const y = new Date().getFullYear()
  return Array.from({ length: count }, (_, i) => y - i)
}
