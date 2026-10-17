# CarPrices

A private, single-user tool to collect vehicle data and final sale prices from Copart and IAAI
(plus free/auxiliary sources) into a local database and run statistics for car-buying decisions.

Full design: [docs/IMPLEMENTATION_PLAN.md](docs/IMPLEMENTATION_PLAN.md).

> **Phase 0:** solution + schema + SQLite + migrations + seed data, a VinCheck aggregate-index
> importer, a statistics engine, a minimal Web API, and a React dashboard.
> **Phase 1:** the Copart collector — a Playwright-based client scoped by collection targets,
> make/model/trim normalization, idempotent Car/Lot upsert, and a close-capture pass for final prices.
> **Phase 2:** enrichment — NHTSA vPIC VIN decode (cached), baseline feature extraction, and
> VIN-from-image recovery (barcode via ZXing, pluggable OCR, check-digit + masked-pattern validation).
> **Phase 3:** the IAAI collector — Playwright + HTML parsing (AngleSharp) behind Incapsula, scoped by
> the same collection targets, with discovery, image-URL capture, and best-effort close-capture.
> **Phase 4:** analytics — PriceStats rollups and FeaturePriceImpact from captured sold lots, plus a
> comparable-based price estimator, surfaced in the API and dashboard.

## Prerequisites

- **.NET 10 SDK** (tested with 10.0.303)
- **Node.js 20 LTS or newer** for the React UI — ⚠️ the machine currently has Node v10, which is
  too old for Vite. Update Node before running the frontend.
- **Playwright browsers** for the Copart collector — after a build, install once:
  `pwsh src/CarPrices.Cli/bin/Debug/net10.0/playwright.ps1 install chromium`

## Solution layout

```
src/
  CarPrices.Domain          Entities + enums (no dependencies)
  CarPrices.Infrastructure  EF Core DbContext, migrations, SQLite, seed data
  CarPrices.Scraping        VinCheck importer + Copart (JSON) & IAAI (HTML) collectors
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
stats rollup                    Recompute PriceStats + FeaturePriceImpact from sold lots
makes                           List makes with observation counts
targets add --make <m> [--model <m>] [--year-from <y>] [--sources <a,b>]
targets list                    List collection targets
targets sync                    Load collection targets from config into the DB
scrape copart                   Run Copart discovery for enabled targets (needs Playwright)
scrape iaai                     Run IAAI discovery for enabled targets (needs Playwright)
capture-close [iaai]            Re-check closed lots to record final sale prices
enrich                          vPIC VIN decode + baseline feature extraction
vin-from-image --file <p> [--lot <id>] [--pattern <11>]   Recover a VIN from an image
```

## Copart collector (Phase 1)

The collector is **scoped by collection targets** so it only pulls the cars you care about, never the
whole catalog. Targets come from the `Collection` config section (API/Worker) or the CLI:

```bash
# define what to collect
dotnet run --project src/CarPrices.Cli -- targets add --make BMW --model "3 Series" --year-from 2020 --sources Copart
dotnet run --project src/CarPrices.Cli -- targets list

# install the browser once, then collect
pwsh src/CarPrices.Cli/bin/Debug/net10.0/playwright.ps1 install chromium
dotnet run --project src/CarPrices.Cli -- scrape copart      # discovery: upsert current lots
dotnet run --project src/CarPrices.Cli -- capture-close      # record final prices for closed lots
```

How it works:

- **CopartClient** (`CarPrices.Scraping/Copart`) drives real Chromium via Playwright, warms a page to
  pick up the Imperva/Incapsula cookies, then calls `/public/lots/search-results` *from inside the page*
  (plain HTTP clients get challenged). Pagination is capped at 50 pages/query; delays are jittered.
- **CopartLotParser** decodes the short-key JSON into `ScrapedLot`; **LotIngestionService** resolves
  make/model/trim via the alias tables and upserts Cars + Lots + RawPayload, idempotent on
  (Source, LotNumber, AuctionDate). Cars are deduped by full VIN when one is present.
- **Close-capture** re-checks lots whose auction has passed and records the final bid as `SalePrice`.
- The **Worker** can run discovery + close-capture on a schedule — set `Worker:Enabled=true`.

> ⚠️ The exact Copart field meanings and end-of-sale status codes were inferred from live responses;
> the close-capture `SaleStatus` heuristics (`CopartCollector.ClassifyFinal`) should be tuned against
> real sold lots. Raw payloads are retained so lots can be re-normalized without re-scraping.

### IAAI collector (Phase 3)

```bash
dotnet run --project src/CarPrices.Cli -- scrape iaai      # discovery
dotnet run --project src/CarPrices.Cli -- capture-close iaai
```

IAAI serves its vehicle list as **server-rendered HTML** (no clean JSON feed), so `IaaiClient`
(Playwright) fetches the search page and `IaaiLotParser` (AngleSharp) extracts each card. The reliable
identity fields (VIN, year, make/model/trim, item id, branch, auction date, status) come straight from
the card's `onclick`/`href` attributes; body fields (odometer, damage, prices, engine) are parsed
best-effort from the `data-list` text. IAAI exposes image URLs directly (`vis.iaai.com/resizer?...`),
which are stored as `LotImages` for the VIN-from-image step.

> ⚠️ IAAI body-field parsing and the close-capture `DetectSold` heuristic were built from a live card
> sample and should be verified against more pages; pagination is best-effort (stops when a page
> yields no new lots).

## Enrichment (Phase 2)

Once lots are collected, enrichment fills in specs and features:

```bash
dotnet run --project src/CarPrices.Cli -- enrich      # decode VINs (vPIC) + extract features
```

- **VIN decode** — `VinDecodeService` calls the free NHTSA vPIC `DecodeVINValuesBatch` endpoint and
  caches results in `VinDecodes`. It fills Car body/drivetrain/fuel/engine and cross-fills
  make/model/trim via the taxonomy. Works on a full VIN or an 11-char VIN pattern.
- **Features** — `FeatureExtractor` maps vPIC safety/drivetrain fields (adaptive cruise, blind spot,
  lane keep, backup camera, AWD…) and free-text options (via `FeatureMappings`) to normalized
  `CarFeatures`, each tagged with its source and a confidence.

### VIN-from-image recovery

Copart masks VINs in text but prints the full VIN in lot photos. `CarPrices.Vision` recovers it:

```bash
dotnet run --project src/CarPrices.Cli -- vin-from-image --file label.jpg --pattern 4T1BF3EK2AU
```

- **Barcode first** (`BarcodeVinReader`, ZXing + SkiaSharp) — the most reliable path when a VIN
  barcode (Code 39/128, Data Matrix, PDF417, QR) is present.
- **OCR fallback** — pluggable via `IOcrEngine` (default `NullOcrEngine`; drop in Tesseract or a cloud engine).
- **Validation** — a candidate is accepted only if it is check-digit valid **and** its prefix matches
  the masked VIN from the listing. Every attempt is logged to `VinExtractions`; an accepted VIN is
  written back to the Car with `VinSource` = PhotoBarcode/PhotoOcr.

## Analytics (Phase 4)

Once sold prices are captured (collectors + close-capture), compute and explore analytics:

```bash
dotnet run --project src/CarPrices.Cli -- stats rollup   # recompute PriceStats + FeaturePriceImpact
```

- **PriceStats** (`RollupService`) — percentile rollups (min/mean/median/P25/P75/P90/stddev + mean
  odometer) by maker, maker+model, and maker+model+year over sold lots (min sample size enforced).
- **FeaturePriceImpact** (`FeatureImpactService`) — mean sold price *with* vs *without* each feature
  within a maker+model segment, with absolute and percentage deltas.
- **Price estimator** (`EstimatorService`) — finds comparable sold lots (maker/model/trim, year within
  a tolerance, optional title/damage group) and returns the distribution + a suggested price, with an
  optional linear mileage adjustment.

API: `GET /api/stats/sold-overview`, `GET /api/stats/price-stats`, `GET /api/stats/feature-impact`,
`POST /api/stats/rollup`, `POST /api/estimate`. The dashboard adds a captured-sold-data overview and a
price-estimator panel.

## Live run status (first end-to-end run, 2026-10-17)

Validated against live sources:

- **IAAI discovery works headless** — real BMW 3 Series lots parsed and ingested (identity, VIN
  pattern, damage, title, odometer, image URLs).
- **vPIC enrichment works** — VIN patterns decoded; specs + ~9 features/car extracted (adaptive
  cruise, blind spot, AEB, backup camera, AWD/RWD, …).
- **Analytics** compute correctly once sold prices exist.

Needs tuning / known limits:

- **Copart is blocked headless** — Incapsula returns HTTP 403 to the headless-shell browser. It needs
  a **headed** full-Chromium browser on an interactive desktop (`Copart:Headless=false`, run the
  CLI/Worker from your own session) or a **residential proxy**. Warm-up now uses `WaitUntil=Commit`
  so it no longer times out on Copart's homepage.
- **IAAI body fields** — title and damage are classified by keyword (reliable); bid vs buy-now price,
  a few placeholder-card odometers, and buy-now-only statuses still need DOM-targeted parsing.
- **Close-capture / sold prices** — current lots are all *upcoming*, so there are no sold prices yet;
  the `ClassifyFinal` / `DetectSold` heuristics can only be tuned once lots actually close.
- Use `dotnet run --project src/CarPrices.Cli -- dump` to inspect captured cars / lots / features.

## Historical sold prices (carsbidshistory)

Copart/IAAI only list *current* lots, so the 3-year sold-price history comes from the
carsbidshistory.com archive (free, Copart-sourced, data back to 2017):

```bash
dotnet run --project src/CarPrices.Cli -- scrape history --make BMW --model 330i --year-from 2020
```

- `CarsBidsHistoryParser` resolves the make id, matches model(s), paginates each model's listing, and
  parses each car's per-VIN **bid-history table** into one sold `Lot` (the latest sale) with full VIN,
  sale price, date, odometer, and damage.
- `--year-from` / `--year-to` filter by model year; `--max-cars` caps detail fetches per model.
- ⚠️ carsbidshistory files trims as separate "models" — the generic `3 SERIES` entry skews to old
  salvage, so scrape specific trims (`330i`, `340i`, `m340i`) for modern cars.
- Sold lots feed PriceStats, the estimator, and the inventory browse. (Photos on that site are
  JS-loaded, so history lots currently show no thumbnail; current IAAI lots do.)
- Verified live: BMW 330i + 3 Series 2020+ → **62 sold lots, median ~$13,800, 2020–2025**.

## Notes

- `data/sample/vincheck-sample.csv` is **illustrative sample data**, not real auction results.
- The live VinCheck URL in `appsettings.json` is a placeholder — confirm the real download URL/format
  before relying on live import; the importer tolerates common CSV/JSON column-name variants.
- Database migrations live in `src/CarPrices.Infrastructure/Migrations`. Add one with:
  `dotnet ef migrations add <Name> --project src/CarPrices.Infrastructure --startup-project src/CarPrices.Infrastructure`
