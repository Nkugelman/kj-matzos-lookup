import type {
  CustomerOrderDetail,
  CustomerOrderLookupResult,
  CustomerProfile,
  CustomerProfileUpdate,
  KioskSetup,
} from "./types"
import { digitsOnly } from "./phoneUtils"

/** Empty = same origin; Vite dev server proxies /api to the kiosk API. */
const BASE = import.meta.env.VITE_API_BASE_URL?.trim() || ""

function apiUrl(path: string) {
  return `${BASE}${path}`
}

interface ApiEnvelope<T> {
  isSuccess?: boolean
  IsSuccess?: boolean
  message?: string
  Message?: string
  response?: T
  Response?: T
  errors?: string[] | string | null
  Errors?: string[] | string | null
}

function envelopeFailed(data: ApiEnvelope<unknown>): boolean {
  return data.isSuccess === false || data.IsSuccess === false
}

function envelopeMessage(data: ApiEnvelope<unknown>, fallback: string): string {
  return data.message || data.Message || fallback
}

function formatErrors(data: ApiEnvelope<unknown>): string[] {
  const err = data.errors ?? data.Errors
  if (Array.isArray(err)) return err.filter((x): x is string => typeof x === "string" && !!x)
  if (typeof err === "string" && err) return [err]
  return []
}

async function readApi<T>(res: Response): Promise<T> {
  let data: ApiEnvelope<T>
  try {
    data = await res.json()
  } catch {
    throw new Error(
      res.ok
        ? `Invalid response from server (${res.status})`
        : `Server error (${res.status}). Is the API running on port 5041?`
    )
  }

  if (!res.ok || envelopeFailed(data)) {
    const parts = [envelopeMessage(data, res.ok ? "Request failed" : `Server error (${res.status})`)]
    parts.push(...formatErrors(data))
    throw new Error(parts.join(" — "))
  }

  const payload = data.response ?? data.Response
  if (payload === undefined || payload === null) {
    throw new Error(envelopeMessage(data, "Empty response from server"))
  }
  return payload
}

export function formatFetchError(err: unknown, context: string): string {
  if (err instanceof Error) {
    if (err.message === "Failed to fetch" || err.name === "TypeError") {
      return `${context} Start the API (dotnet run in api/KjMatzosLookup.Api) and ensure VPN if using the KJ Matzos database.`
    }
    return err.message
  }
  return `${context} Try again.`
}

export async function fetchSetup(): Promise<KioskSetup> {
  const res = await fetch(apiUrl("/api/kiosk/customer-orders/setup"))
  return readApi<KioskSetup>(res)
}

export async function lookupOrders(
  customerId: number,
  phone: string,
  options?: { year?: number | null; hebrewYear?: number | null; take?: number }
): Promise<CustomerOrderLookupResult> {
  const params = new URLSearchParams({
    customerId: String(customerId),
    phone: digitsOnly(phone),
    take: String(options?.take ?? 100),
  })
  if (options?.year) params.set("year", String(options.year))
  if (options?.hebrewYear) params.set("hebrewYear", String(options.hebrewYear))
  const res = await fetch(apiUrl(`/api/kiosk/customer-orders/orders?${params}`))
  return readApi<CustomerOrderLookupResult>(res)
}

export async function updateProfile(body: CustomerProfileUpdate): Promise<CustomerProfile> {
  const res = await fetch(apiUrl("/api/kiosk/customer-orders/profile"), {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({
      ...body,
      phone: digitsOnly(body.phone),
    }),
  })
  return readApi<CustomerProfile>(res)
}

export async function fetchOrderDetail(
  customerId: number,
  transactionId: string,
  shopperCustomerId: string
): Promise<CustomerOrderDetail> {
  const params = new URLSearchParams({
    customerId: String(customerId),
    shopperCustomerId,
  })
  const res = await fetch(
    apiUrl(`/api/kiosk/customer-orders/orders/${transactionId}?${params}`)
  )
  return readApi<CustomerOrderDetail>(res)
}
