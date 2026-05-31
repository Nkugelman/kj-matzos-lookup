import type { PurchaseYear } from "./types"

export function purchaseYearsToOptions(
  purchaseYears: PurchaseYear[] | undefined
): HebrewYearOption[] {
  return (purchaseYears ?? []).map((y) => ({
    label: y.hebrewLabel,
    hebrewYear: y.hebrewYear,
  }))
}

export interface HebrewYearOption {
  label: string
  hebrewYear: number
}

export function yearOfLabel(hebrewLabel: string): string {
  return `שנת ${hebrewLabel}`
}

export function yearTotalHeader(hebrewLabel: string): string {
  return `סך הכל פונט וואס איר האט גענומען פאר די יאר ${hebrewLabel}`
}
