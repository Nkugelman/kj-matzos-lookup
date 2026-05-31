import { StrictMode, useCallback, useEffect, useMemo, useState } from "react"
import { createRoot } from "react-dom/client"
import {
  fetchOrderDetail,
  fetchSetup,
  formatFetchError,
  lookupOrders,
  updateProfile,
} from "./api"
import {
  KioskEditProfile,
  profileToForm,
  type ProfileFormState,
} from "./KioskEditProfile"
import {
  purchaseYearsToOptions,
  yearOfLabel,
  yearTotalHeader,
  type HebrewYearOption,
} from "./hebrewYearUtils"
import { KioskPrintSlip, printKioskSlip, type PrintMode } from "./KioskPrintSlip"
import { COPY } from "./kioskCopy"
import {
  digitsOnly,
  formatPhoneInput,
  isValidPhone,
} from "./phoneUtils"
import {
  aggregateYearLines,
  formatPounds,
  formatReceiptEnglishDate,
  formatReceiptHebrewDate,
  formatReceiptLine,
  type YearLineTotal,
} from "./receiptUtils"
import type {
  AppStep,
  CustomerOrderDetail,
  CustomerOrderLookupResult,
  CustomerOrderSummary,
  KioskSetup,
} from "./types"
import "./index.css"

const IDLE_MS = 2 * 60 * 1000
const KEYPAD = ["1", "2", "3", "4", "5", "6", "7", "8", "9", "*", "0", "#"]

function SearchIcon() {
  return (
    <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" aria-hidden>
      <circle cx="11" cy="11" r="7" />
      <path d="M20 20l-4-4" strokeLinecap="round" />
    </svg>
  )
}

function FabButton({
  label,
  className,
  onClick,
  disabled,
}: {
  label: string
  className: string
  onClick: () => void
  disabled?: boolean
}) {
  const lines = label.split(" ")
  return (
    <button type="button" className={`kiosk-fab ${className}`} onClick={onClick} disabled={disabled}>
      {lines.length > 1 ? (
        lines.map((line) => (
          <span key={line} className="kiosk-fab__stack">
            {line}
          </span>
        ))
      ) : (
        label
      )}
    </button>
  )
}

function App() {
  const [step, setStep] = useState<AppStep>("phone")

  const [kjSetup, setKjSetup] = useState<KioskSetup | null>(null)
  const [readyLoading, setReadyLoading] = useState(true)
  const [readyError, setReadyError] = useState<string | null>(null)

  const [phone, setPhone] = useState("")
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [lookup, setLookup] = useState<CustomerOrderLookupResult | null>(null)
  const [selectedYear, setSelectedYear] = useState<HebrewYearOption | null>(null)
  const [visitReceipts, setVisitReceipts] = useState<CustomerOrderDetail[]>([])
  const [yearTotals, setYearTotals] = useState<YearLineTotal[]>([])
  const [showAllYears, setShowAllYears] = useState(false)
  const [printMode, setPrintMode] = useState<PrintMode | null>(null)
  const [profileForm, setProfileForm] = useState<ProfileFormState | null>(null)
  const [profileSaving, setProfileSaving] = useState(false)
  const [profileSaved, setProfileSaved] = useState(false)

  const purchaseYearOptions = useMemo(
    () => purchaseYearsToOptions(lookup?.purchaseYears),
    [lookup?.purchaseYears]
  )
  const visibleYears = showAllYears
    ? purchaseYearOptions
    : purchaseYearOptions.slice(0, 3)

  useEffect(() => {
    setReadyLoading(true)
    setReadyError(null)
    fetchSetup()
      .then(setKjSetup)
      .catch((err: unknown) => {
        setReadyError(formatFetchError(err, "Could not connect to the store."))
      })
      .finally(() => setReadyLoading(false))
  }, [])

  const resetCustomerSession = useCallback(() => {
    setStep("phone")
    setPhone("")
    setError(null)
    setLookup(null)
    setSelectedYear(null)
    setVisitReceipts([])
    setYearTotals([])
    setShowAllYears(false)
    setPrintMode(null)
    setProfileForm(null)
    setProfileSaving(false)
    setProfileSaved(false)
  }, [])

  useEffect(() => {
    if (!kjSetup || readyLoading) return
    let timer: ReturnType<typeof setTimeout>
    const bump = () => {
      clearTimeout(timer)
      timer = setTimeout(resetCustomerSession, IDLE_MS)
    }
    bump()
    const events = ["mousedown", "keydown", "touchstart", "scroll"] as const
    events.forEach((e) => window.addEventListener(e, bump))
    return () => {
      clearTimeout(timer)
      events.forEach((e) => window.removeEventListener(e, bump))
    }
  }, [kjSetup, readyLoading, resetCustomerSession])

  const appendDigit = (key: string) => {
    setError(null)
    if (key === "*" || key === "#") return
    setPhone((p) => formatPhoneInput(digitsOnly(p) + key))
  }

  const backspace = () => {
    setError(null)
    setPhone((p) => formatPhoneInput(digitsOnly(p).slice(0, -1)))
  }

  const runPhoneLookup = async (phoneOverride?: string) => {
    if (!kjSetup) return
    const dial = phoneOverride ?? phone
    if (!isValidPhone(dial)) {
      setError("Enter a 10-digit phone number.")
      return
    }
    if (phoneOverride) setPhone(formatPhoneInput(digitsOnly(phoneOverride)))
    setLoading(true)
    setError(null)
    try {
      const result = await lookupOrders(kjSetup.customerId, dial, { take: 1 })
      if (!result.found) {
        setError("No customer found for this phone number.")
        return
      }
      setLookup(result)
      setProfileForm(result.profile ? profileToForm(result.profile) : null)
      setStep("profile")
    } catch (err: unknown) {
      setError(formatFetchError(err, "Could not look up phone number."))
    } finally {
      setLoading(false)
    }
  }

  const loadYearReceipts = async (year: HebrewYearOption) => {
    if (!kjSetup || !lookup?.customerId) return
    setLoading(true)
    setError(null)
    try {
      const result = await lookupOrders(kjSetup.customerId, phone, {
        hebrewYear: year.hebrewYear,
        take: 100,
      })
      setLookup((prev) => (prev ? { ...prev, orders: result.orders, totalCount: result.totalCount } : result))

      const shopperId = lookup.customerId
      const loadDetail = (order: CustomerOrderSummary) =>
        fetchOrderDetail(kjSetup.customerId, order.transactionId, shopperId)

      const visitDetails = await Promise.all(result.orders.map(loadDetail))
      setVisitReceipts(visitDetails)
      setYearTotals(aggregateYearLines(visitDetails))
      setSelectedYear(year)
      setStep("receipts")
    } catch (err: unknown) {
      setError(formatFetchError(err, "Could not load orders for this year."))
    } finally {
      setLoading(false)
    }
  }

  const kioskReady = !!kjSetup && !readyLoading && !readyError
  const sampleCustomers = kjSetup?.sampleCustomers ?? []
  const profile = lookup?.profile
  const displayName = profile?.displayName ?? lookup?.customerName ?? ""
  const phones = profile?.phones?.length
    ? profile.phones.map((p) => formatPhoneInput(digitsOnly(p)))
    : lookup?.phone
      ? [lookup.phone]
      : [formatPhoneInput(digitsOnly(phone))]

  const yearTotalPounds = yearTotals.reduce((n, row) => n + row.pounds, 0)

  const showFabTop = step !== "phone" && kioskReady
  const showFabBack = step === "profile" || step === "receipts"

  const saveProfileField = async (
    field: keyof ProfileFormState,
    value: string
  ) => {
    if (!kjSetup || !lookup?.customerId || !profileForm) return
    const nextForm = { ...profileForm, [field]: value }
    setProfileForm(nextForm)
    setProfileSaving(true)
    setProfileSaved(false)
    setError(null)
    try {
      const updated = await updateProfile({
        customerId: kjSetup.customerId,
        phone,
        shopperCustomerId: lookup.customerId,
        firstName: nextForm.firstName,
        lastName: nextForm.lastName,
        email: nextForm.email,
        address: nextForm.address,
        primaryPhone: digitsOnly(nextForm.primaryPhone),
        secondaryPhone: digitsOnly(nextForm.secondaryPhone),
      })
      setLookup((prev) =>
        prev
          ? {
              ...prev,
              profile: updated,
              customerName: updated.displayName,
              phone: updated.primaryPhone ?? prev.phone,
            }
          : prev
      )
      setProfileForm(profileToForm(updated))
      setProfileSaved(true)
    } catch (err: unknown) {
      setError(formatFetchError(err, "Could not save profile."))
    } finally {
      setProfileSaving(false)
    }
  }

  return (
    <div className={`kiosk ${step === "receipts" ? "kiosk--white" : ""}`}>
      {showFabTop && (
        <FabButton
          label={COPY.startNewPhone}
          className="kiosk-fab--top-right"
          onClick={resetCustomerSession}
          disabled={loading}
        />
      )}
      {showFabBack && (
        <FabButton
          label={COPY.back}
          className="kiosk-fab--bottom-left"
          onClick={() => {
            setError(null)
            if (step === "receipts") {
              setStep("profile")
              setSelectedYear(null)
              setVisitReceipts([])
              setYearTotals([])
            } else {
              setStep("phone")
            }
          }}
          disabled={loading || profileSaving}
        />
      )}

      <div className="kiosk__shell">
        <main className="kiosk__main">
          {readyError && <div className="kiosk-error">{readyError}</div>}
          {error && <div className="kiosk-error">{error}</div>}

          {step === "phone" && (
            <section className="kiosk-phone">
              {readyLoading ? (
                <p className="kiosk-loading">Connecting…</p>
              ) : (
                <>
                  <p className="kiosk-phone__intro">{COPY.phoneIntro}</p>
                  <div className="kiosk-phone__display">
                    <span className="kiosk-phone__number">
                      {phone || "\u00A0"}
                    </span>
                    <button
                      type="button"
                      className="kiosk-phone__backspace"
                      onClick={backspace}
                      disabled={!kioskReady || !phone}
                      aria-label="Backspace"
                    >
                      ⌫
                    </button>
                  </div>
                  <div className="kiosk-keypad">
                    {KEYPAD.map((key) => (
                      <button
                        key={key}
                        type="button"
                        className="kiosk-keypad__key"
                        onClick={() => appendDigit(key)}
                        disabled={!kioskReady}
                      >
                        {key}
                      </button>
                    ))}
                  </div>
                  <button
                    type="button"
                    className="kiosk-search"
                    disabled={loading || !kioskReady || !isValidPhone(phone)}
                    onClick={() => void runPhoneLookup()}
                    aria-label="Search"
                  >
                    <SearchIcon />
                  </button>
                </>
              )}
            </section>
          )}

          {step === "profile" && lookup && (
            <section className="kiosk-profile">
              <p className="kiosk-banner">{COPY.infoForPhone}</p>
              <h1 className="kiosk-profile__title">{COPY.yourProfile}</h1>
              <hr className="kiosk-profile__rule" />
              <p className="kiosk-profile__name">{displayName}</p>
              {phones.map((p) => (
                <p key={p} className="kiosk-profile__line">
                  {p}
                </p>
              ))}
              {profile?.email && (
                <p className="kiosk-profile__line kiosk-profile__email">{profile.email}</p>
              )}
              {profile?.address && (
                <p className="kiosk-profile__line">{profile.address}</p>
              )}
              <button
                type="button"
                className="kiosk-profile__edit"
                disabled={loading || !profile}
                onClick={() => {
                  if (profile) {
                    setProfileForm(profileToForm(profile))
                    setProfileSaved(false)
                    setStep("edit")
                  }
                }}
              >
                {COPY.editOptional}
              </button>
              <p className="kiosk-profile__years-label">{COPY.yearsHeader}</p>
              {purchaseYearOptions.length === 0 ? (
                <p className="kiosk-profile__empty">{COPY.noPurchaseYears}</p>
              ) : (
                <>
                  <div className="kiosk-year-row">
                    {visibleYears.map((y) => (
                      <button
                        key={y.hebrewYear}
                        type="button"
                        className="kiosk-year-btn"
                        disabled={loading}
                        onClick={() => void loadYearReceipts(y)}
                      >
                        {y.label}
                      </button>
                    ))}
                  </div>
                  {purchaseYearOptions.length > 3 && !showAllYears && (
                    <button
                      type="button"
                      className="kiosk-profile__more-years"
                      disabled={loading}
                      onClick={() => setShowAllYears(true)}
                    >
                      {COPY.seeMoreYears}
                    </button>
                  )}
                </>
              )}
              {loading && <p className="kiosk-loading">Loading…</p>}
            </section>
          )}

          {step === "edit" && lookup && profile && profileForm && (
            <KioskEditProfile
              profile={profile}
              form={profileForm}
              saving={profileSaving}
              error={error}
              saved={profileSaved}
              onSaveField={(field, value) => saveProfileField(field, value)}
              onBack={() => {
                setError(null)
                setStep("profile")
              }}
            />
          )}

          {step === "receipts" && lookup && selectedYear && (
            <section className="kiosk-receipts">
              <p className="kiosk-banner">{COPY.infoForPhone}</p>
              {loading ? (
                <p className="kiosk-loading">Loading receipts…</p>
              ) : (
                <>
                  <div className="kiosk-receipts__scroll">
                    {visitReceipts.length === 0 ? (
                      <p className="kiosk-receipts__empty">{COPY.noVisitsThisYear}</p>
                    ) : (
                      visitReceipts.map((detail) => (
                        <div key={detail.transactionId} className="kiosk-receipt-col">
                          <p className="kiosk-receipt-col__name">{displayName}</p>
                          <p className="kiosk-receipt-col__year">{yearOfLabel(selectedYear.label)}</p>
                          <p className="kiosk-receipt-col__label">{COPY.receipt}</p>
                          <div className="kiosk-receipt-box">
                            <div className="kiosk-receipt-box__hebrew">
                              {formatReceiptHebrewDate(detail.saleDate)}
                            </div>
                            <div className="kiosk-receipt-box__english">
                              {formatReceiptEnglishDate(detail.saleDate)}
                            </div>
                            {detail.lines.map((line, i) => (
                              <div key={`${line.description}-${i}`} className="kiosk-receipt-line">
                                {formatReceiptLine(line.description, line.qty, COPY.pounds)}
                              </div>
                            ))}
                          </div>
                          <button
                            type="button"
                            className="kiosk-btn-print"
                            onClick={() =>
                              printKioskSlip(() =>
                                setPrintMode({ kind: "visit", detail })
                              )
                            }
                          >
                            {COPY.print}
                          </button>
                        </div>
                      ))
                    )}
                  </div>

                  <div className="kiosk-summary">
                    <p className="kiosk-summary__head">
                      {yearTotalHeader(selectedYear.label)}
                    </p>
                    <table className="kiosk-summary__table">
                      <tbody>
                        {yearTotals.map((row) => (
                          <tr key={row.description}>
                            <td>{formatPounds(row.pounds)}</td>
                            <td>{row.description}</td>
                          </tr>
                        ))}
                        <tr className="kiosk-summary__total">
                          <td>
                            {selectedYear.label} {formatPounds(yearTotalPounds)} {COPY.pounds}
                          </td>
                          <td>{COPY.total}</td>
                        </tr>
                      </tbody>
                    </table>
                    <button
                      type="button"
                      className="kiosk-btn-print-total"
                      onClick={() =>
                        printKioskSlip(() =>
                          setPrintMode({
                            kind: "total",
                            year: selectedYear,
                            rows: yearTotals,
                            totalPounds: yearTotalPounds,
                          })
                        )
                      }
                    >
                      {COPY.printTotalOnly}
                    </button>
                  </div>
                </>
              )}
            </section>
          )}
        </main>

        {sampleCustomers.length > 0 && (
          <aside className="sample-panel" aria-label="Sample test customers">
            <h2 className="sample-panel__title">Test customers</h2>
            <p className="sample-panel__hint">Dev only — tap to look up</p>
            <ul className="sample-panel__list">
              {sampleCustomers.map((sample) => (
                <li key={`${sample.phone}-${sample.label}`}>
                  <button
                    type="button"
                    className="sample-panel__item"
                    onClick={() => void runPhoneLookup(sample.phone)}
                    disabled={loading || !kioskReady}
                  >
                    <span className="sample-panel__label">{sample.label}</span>
                    <span className="sample-panel__phone">
                      {formatPhoneInput(digitsOnly(sample.phone))}
                    </span>
                    {sample.note && (
                      <span className="sample-panel__note">{sample.note}</span>
                    )}
                  </button>
                </li>
              ))}
            </ul>
          </aside>
        )}
      </div>

      <KioskPrintSlip
        mode={printMode}
        profile={profile ?? null}
        displayName={displayName}
        phones={phones}
      />
    </div>
  )
}

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <App />
  </StrictMode>
)
