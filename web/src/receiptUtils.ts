import type { CustomerOrderDetail, CustomerOrderLine } from "./types"

export function formatPounds(qty: number): string {
  return qty.toFixed(2)
}

export function formatReceiptLine(description: string, qty: number, poundsLabel: string): string {
  return `${description} ${formatPounds(qty)} ${poundsLabel}`
}

export function formatReceiptEnglishDate(value?: string): string {
  if (!value) return ""
  const d = new Date(value)
  if (Number.isNaN(d.getTime())) return value
  return d.toLocaleDateString("en-US", { month: "short", day: "numeric", year: "numeric" })
}

export function formatReceiptHebrewDate(value?: string): string {
  if (!value) return ""
  const d = new Date(value)
  if (Number.isNaN(d.getTime())) return ""
  const day = d.toLocaleDateString("en-US", { weekday: "long" })
  const month = d.toLocaleDateString("en-US", { month: "long", day: "numeric" })
  return `${day} · ${month}`
}

export interface YearLineTotal {
  description: string
  pounds: number
}

export function aggregateYearLines(details: CustomerOrderDetail[]): YearLineTotal[] {
  const map = new Map<string, number>()
  for (const detail of details) {
    for (const line of detail.lines) {
      const key = line.description.trim() || "Item"
      map.set(key, (map.get(key) ?? 0) + line.qty)
    }
  }
  return [...map.entries()]
    .map(([description, pounds]) => ({ description, pounds }))
    .sort((a, b) => a.description.localeCompare(b.description))
}

export function sumPounds(lines: CustomerOrderLine[]): number {
  return lines.reduce((n, l) => n + l.qty, 0)
}
