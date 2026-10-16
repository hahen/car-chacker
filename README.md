# CarPrices

A private, single-user tool to collect vehicle data and final sale prices from Copart and IAAI
(plus free/auxiliary sources) into a local database and run statistics for car-buying decisions.

Full design: [docs/IMPLEMENTATION_PLAN.md](docs/IMPLEMENTATION_PLAN.md).

> **Phase 0 (this scaffold):** solution + schema + SQLite + migrations + seed data, a VinCheck
> aggregate-index importer, a basic statistics engine, a minimal Web API, and a React dashboard.
> Scraping (Copart/IAAI), VIN decode, VIN-from-image recovery, and the live price collector come
> in later phases.

## Prerequisites

- **.NET 10 SDK** (tested with 10.0.303)
- **Node.js 20 LTS or newer** for the React UI — ⚠️ the machine currently has Node v10, which is
  too old for Vite. Update Node before running the frontend.

## Solution layout

```
src/
  CarPrices.Domain          Entities + enums (no dependencies)
  CarPrices.Infrastructure  EF Core DbContext, migrations, SQLite, seed data
  CarPrices.Scraping        VinCheck importer (Copart/IAAI clients come later)
  CarPrices.Enrichment      VIN decode / feature extraction (later)
  CarPrices.Vision          VIN-from-image: barcode + OCR (later)
  CarPrices.Normalization   maker/model/trim + feature normalization (later)
  CarPrices.Statistics      percentile engine + stats service
  CarPrices.Api             ASP.NET Core minimal API (serves the UI)
  CarPrices.Worker          scheduled jobs host (later)
  CarPrices.Cli             ops commands (migrate, import, stats)
web/                        React + Vite + TypeScript dashboard
data/                       SQLite db (car.db) + sample data  (db is git-ignored)
```

## Quick start

### 1. Database + sample data (CLI)

```bash
dotnet run --project src/CarPrices.Cli -- db migrate
dotnet run --project src/CarPrices.Cli -- import vincheck --sample
dotnet run --project src/CarPrices.Cli -- stats overview
```

The database is created at `data/car.db` (SQLite, WAL mode).

### 2. API

```bash
dotnet run --project src/CarPrices.Api
```

Serves on `http://localhost:5080`. The database is migrated + seeded automatically on startup.
Key endpoints:

- `GET  /api/health`
- `GET  /api/stats/overview`
- `GET  /api/stats/makes-summary`
- `GET  /api/makes` · `GET /api/makes/{make}/models`
- `GET  /api/stats/series?make=BMW&model=3%20Series`
- `POST /api/import/vincheck/sample` · `POST /api/import/vincheck?url=...`

### 3. React dashboard (needs Node 20+)

```bash
cd web
npm install
npm run dev
```

Opens on `http://localhost:5173` and proxies `/api` to the API on `:5080`. Run the API too.
For a production build served by the API: `npm run build` (outputs into `CarPrices.Api/wwwroot`).

## CLI commands

```
db migrate                      Apply migrations + seed lookups
import vincheck --sample        Import the bundled sample index (data/sample)
import vincheck --file <path>   Import a local CSV/JSON index
import vincheck [--url <url>]   Download + import the live index
stats overview                  Print market overview
makes                           List makes with observation counts
```

## Notes

- `data/sample/vincheck-sample.csv` is **illustrative sample data**, not real auction results.
- The live VinCheck URL in `appsettings.json` is a placeholder — confirm the real download URL/format
  before relying on live import; the importer tolerates common CSV/JSON column-name variants.
- Database migrations live in `src/CarPrices.Infrastructure/Migrations`. Add one with:
  `dotnet ef migrations add <Name> --project src/CarPrices.Infrastructure --startup-project src/CarPrices.Infrastructure`
