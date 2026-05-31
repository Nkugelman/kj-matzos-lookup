# KJ Matzos Lookup

In-store kiosk for **KJ Matzos**. Customers enter a phone number to view past orders.

## Repo layout

```
kj-matzos-lookup/
├── web/                  React kiosk UI (port 5180)
├── server/               Kiosk API module (routes + DB lookup)
├── api/KjMatzosLookup.Api/   Standalone API host (port 5041)
└── README.md
```

Requires **BackOffice-Web** cloned as a sibling folder (shared DB libraries):

```
rdt_backOffice/
├── BackOffice-Web/       ← existing back office repo
└── kj-matzos-lookup/     ← this repo
```

## Run locally

**Terminal 1 — API**

```powershell
cd api/KjMatzosLookup.Api
dotnet run
```

**Terminal 2 — Kiosk UI**

```powershell
cd web
npm install
npm run dev
```

Copy `api/KjMatzosLookup.Api/appsettings.Development.json.example` to `appsettings.Development.json` (gitignored) and fill in connection strings.

For the web app, copy `web/.env.example` to `web/.env` if needed. **Leave `VITE_API_BASE_URL` empty** so Vite proxies `/api` to port **5190** (kiosk API; Back Office uses 5041).

| App | URL |
|-----|-----|
| Kiosk | http://localhost:5180 |
| API | http://localhost:5190/api/kiosk/customer-orders/setup |

## Configuration

Edit `api/KjMatzosLookup.Api/appsettings.Development.json`:

```json
"CustomerOrderKiosk": {
  "TenantCustomerId": 152,
  "TenantDisplayName": "KJ Matzos"
}
```

| CustomerId | Tenant | Database |
|-----------:|--------|----------|
| **25** | Develop Self Checkout (default) | `Develop_SelfCheckout` on Azure |
| 152 | KJ Matzos | RDT on `192.168.254.10:1533` (VPN) |
| 150 | KJ Matzos test | KJMatzoh_Test |

Set `CustomerOrderKiosk:TenantCustomerId` in `appsettings.Development.json` to switch tenants.

### Test customers (dev sidebar)

In **Development**, if `SampleCustomers` is empty, the API auto-loads up to 8 shoppers with phones from the tenant DB (`AutoLoadSampleCustomersInDevelopment`, default `true`).

Or add explicit entries to show a **Test customers** panel on the right (returned on `/setup`; omit in production):

```json
"SampleCustomers": [
  { "Label": "Jane — has orders", "Phone": "2125551234", "Note": "optional" }
]
```

Find candidates (run on the tenant database, e.g. `Develop_SelfCheckout`):

```sql
SELECT TOP 20
  c.CustomerID,
  c.FirstName,
  c.LastName,
  c.Phone
FROM CustomerView c
WHERE c.Phone IS NOT NULL AND c.Status > -1
ORDER BY c.LastName, c.FirstName;
```

Restart the API after editing `appsettings.Development.json`.

## Confluence

| Page | Source | Publish |
|------|--------|---------|
| [Customer proposal](https://rdtsystems.atlassian.net/wiki/spaces/KJMATZ/pages/164167681) | `docs/KJ-Matzos-Confluence.md` | `.\docs\publish-confluence.ps1` |
| Developer spec (internal) | `docs/KJ-Matzos-Confluence-Developer.md` | `.\docs\publish-confluence.ps1 -Developer` |

Copy `docs/.env.example` → `docs/.env` and set `CONFLUENCE_API_TOKEN`.
