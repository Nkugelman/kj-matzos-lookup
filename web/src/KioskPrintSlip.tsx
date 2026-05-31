import type { CustomerOrderDetail, CustomerProfile } from "./types"
import { COPY } from "./kioskCopy"
import {
  formatPounds,
  formatReceiptEnglishDate,
  formatReceiptHebrewDate,
  formatReceiptLine,
  type YearLineTotal,
} from "./receiptUtils"
import type { HebrewYearOption } from "./hebrewYearUtils"
import { yearTotalHeader } from "./hebrewYearUtils"

export type PrintMode =
  | { kind: "visit"; detail: CustomerOrderDetail }
  | { kind: "total"; year: HebrewYearOption; rows: YearLineTotal[]; totalPounds: number }

interface KioskPrintSlipProps {
  mode: PrintMode | null
  profile: CustomerProfile | null
  displayName: string
  phones: string[]
}

export function KioskPrintSlip({ mode, profile, displayName, phones }: KioskPrintSlipProps) {
  if (!mode) return null

  const name = profile?.displayName ?? displayName
  const email = profile?.email
  const address = profile?.address
  const phoneLines = profile?.phones?.length ? profile.phones : phones

  return (
    <div className="print-slip" aria-hidden>
      <p className="print-slip__banner">{COPY.infoForPhone}</p>
      <p className="print-slip__name">{name}</p>
      {phoneLines.map((p) => (
        <p key={p} className="print-slip__line">
          {p}
        </p>
      ))}
      {email && <p className="print-slip__line">{email}</p>}
      {address && <p className="print-slip__line">{address}</p>}

      {mode.kind === "visit" && (
        <>
          <hr className="print-slip__rule" />
          <p className="print-slip__label">{COPY.receipt}</p>
          <p className="print-slip__date-hebrew">
            {formatReceiptHebrewDate(mode.detail.saleDate)}
          </p>
          <p className="print-slip__date-english">
            {formatReceiptEnglishDate(mode.detail.saleDate)}
          </p>
          {mode.detail.lines.map((line, i) => (
            <p key={`${line.description}-${i}`} className="print-slip__item">
              {formatReceiptLine(line.description, line.qty, COPY.pounds)}
            </p>
          ))}
        </>
      )}

      {mode.kind === "total" && (
        <>
          <hr className="print-slip__rule" />
          <p className="print-slip__summary-head">{yearTotalHeader(mode.year.label)}</p>
          {mode.rows.map((row) => (
            <p key={row.description} className="print-slip__item">
              {formatReceiptLine(row.description, row.pounds, COPY.pounds)}
            </p>
          ))}
          <p className="print-slip__total">
            {COPY.total}: {mode.year.label} {formatPounds(mode.totalPounds)} {COPY.pounds}
          </p>
        </>
      )}
    </div>
  )
}

export function printKioskSlip(onReady: () => void) {
  onReady()
  requestAnimationFrame(() => {
    window.print()
  })
}
