---
title: KJ Matzos - Developer Spec (Internal)
confluence-space: KJMATZ
confluence-page-id: 167116829
---

# KJ Matzos - Developer Spec (Internal)

**Audience:** RDT development, QA  
**Customer proposal:** [KJ Matzos - In-Store Order Lookup (Proposal)](https://rdtsystems.atlassian.net/wiki/spaces/KJMATZ/pages/164167681)  
**Repo:** [rdt-systems/kj-matzos-lookup](https://github.com/rdt-systems/kj-matzos-lookup)

---

## Customer decisions (implemented / planned)

| Area | Requirement |
|------|-------------|
| **Flow** | Phone → Profile (Screen 2) → Receipts (Screen 3) |
| **Visits** | Horizontal **scroll** for all visits in selected Hebrew year |
| **Years (Screen 2)** | Last **3 Hebrew years with purchases**; **see more** button; skip years with no orders |
| **Language** | **Yiddish only** on tablet UI |
| **Dates** | Screen 2: Hebrew year labels; Screen 3: **Hebrew + English** per visit |
| **Print** | Customer info + visit date + **items/pounds only**; **Yiddish**; **no payment/tax** |
| **Phone** | **Full numbers** on screen and print (not masked) |
| **Search** | Main phone + **cell/alternate** via `CustomerToPhoneView` |
| **Customers** | **Active only** (`Status > -1`); live DB (no cache) |
| **EDIT YOUR INFO** | Optional — per-field edit screen (v1) |
| **POS jump** | **Out of scope** — POS product, not this kiosk |
| **Printers** | Per-device printer config (each tablet → own printer) — **TBD** |

---

## Repo layout

```
kj-matzos-lookup/
  web/                         React kiosk (Vite, 5180)
  server/                      API module + lookup engine
  api/KjMatzosLookup.Api/      Host (5190)
  docs/
```

Requires **BackOffice-Web** sibling for `MainDBContext`, `TenantDbContextFactory`.

---

## Run locally

```powershell
cd api/KjMatzosLookup.Api && dotnet run
cd web && npm run dev
```

| Service | URL |
|---------|-----|
| Kiosk | http://localhost:5180 |
| API | http://localhost:5190/api/kiosk/customer-orders/setup |

---

## API

`GET /api/kiosk/customer-orders/orders?customerId=&phone=&hebrewYear=&take=`  
`PUT /api/kiosk/customer-orders/profile` — update name, email, address, phones (phone-verified)

| Field | Notes |
|-------|--------|
| `phone` | Full formatted number in response (not masked) |
| `profile` | Name, phones[], email, address |
| `purchaseYears[]` | `{ hebrewYear, hebrewLabel }` — years with completed transactions |
| `hebrewYear` query | Filter orders by Hebrew year of `StartSaleTime` |

Shopper lookup: `CustomerView` + `CustomerToPhoneView` (phone **and cell**). Active customers only.

---

## Hebrew year logic

`HebrewYearHelper` (server) and `hebrewYearUtils` (web): Tishrei-based approximation from transaction date.

Screen 2 buttons = top 3 from `purchaseYears`, optional expand for rest.

---

## Print (current)

Browser `window.print()` with hidden `.print-slip` — customer info, Yiddish headers, items/pounds only.

**Next:** per-tablet printer routing (customer answer #2).

---

## Build checklist

- [x] ItemSale UI flow (phone / profile / receipts)
- [x] Purchase-based Hebrew years + see more
- [x] Scroll all visits
- [x] Full phone on screen
- [x] Print slip — customer info + pounds, Yiddish, no payment
- [ ] Per-device receipt printer config
- [ ] SQL-side phone search (performance)
- [ ] Full Hebrew calendar dates on Screen 3 (beyond English weekday/month placeholder)
- [x] EDIT YOUR INFO (optional per-field screen)

---

## Publish docs

```powershell
.\docs\publish-confluence.ps1
.\docs\publish-confluence.ps1 -Developer
```
