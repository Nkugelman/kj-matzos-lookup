export const DEFAULT_KIOSK_NAME = "Order Lookup"

export interface KioskSampleCustomer {
  label: string
  phone: string
  note?: string
}

export interface KioskSetup {
  customerId: number
  customerName: string
  sampleCustomers?: KioskSampleCustomer[]
}

export interface CustomerOrderSummary {
  transactionId: string
  transactionNo: string
  saleDate?: string
  storeId?: string
  storeName?: string
  total: number
  itemCount: number
  statusLabel: string
  isPhoneOrder: boolean
}

export interface CustomerOrderLine {
  description: string
  qty: number
  lineTotal: number
}

export interface CustomerOrderDetail {
  transactionId: string
  transactionNo: string
  saleDate?: string
  storeName?: string
  subtotal: number
  tax: number
  total: number
  statusLabel: string
  lines: CustomerOrderLine[]
}

export interface CustomerProfile {
  firstName?: string
  lastName?: string
  displayName: string
  phones: string[]
  primaryPhone?: string
  secondaryPhone?: string
  secondaryPhoneLinkId?: string
  email?: string
  address?: string
}

export interface CustomerProfileUpdate {
  customerId: number
  phone: string
  shopperCustomerId: string
  firstName?: string
  lastName?: string
  email?: string
  address?: string
  primaryPhone?: string
  secondaryPhone?: string
}

export interface PurchaseYear {
  hebrewYear: number
  hebrewLabel: string
}

export interface CustomerOrderLookupResult {
  found: boolean
  phone?: string
  maskedPhone?: string
  customerId?: string
  customerName?: string
  profile?: CustomerProfile
  purchaseYears?: PurchaseYear[]
  orders: CustomerOrderSummary[]
  totalCount: number
}

export type AppStep = "phone" | "profile" | "edit" | "receipts"
