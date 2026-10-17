import type {
  CatalogFacet,
  EstimateResult,
  ImportResult,
  LotSearchResult,
  MakeSummary,
  MarketOverview,
  ModelPriceSeries,
  SoldOverview,
} from './types'

async function get<T>(path: string): Promise<T> {
  const r = await fetch(path)
  if (!r.ok) throw new Error(`${path} -> ${r.status}`)
  return r.json() as Promise<T>
}

async function post<T>(path: string): Promise<T> {
  const r = await fetch(path, { method: 'POST' })
  if (!r.ok) throw new Error(`${path} -> ${r.status}`)
  return r.json() as Promise<T>
}

async function postJson<T>(path: string, body: unknown): Promise<T> {
  const r = await fetch(path, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  })
  if (!r.ok) throw new Error(`${path} -> ${r.status}`)
  return r.json() as Promise<T>
}

export const api = {
  overview: () => get<MarketOverview>('/api/stats/overview'),
  makesSummary: () => get<MakeSummary[]>('/api/stats/makes-summary'),
  makes: () => get<string[]>('/api/makes'),
  models: (make: string) => get<string[]>(`/api/makes/${encodeURIComponent(make)}/models`),
  series: (make: string, model: string) =>
    get<ModelPriceSeries>(
      `/api/stats/series?make=${encodeURIComponent(make)}&model=${encodeURIComponent(model)}`,
    ),
  importSample: () => post<ImportResult>('/api/import/vincheck/sample'),
  soldOverview: () => get<SoldOverview>('/api/stats/sold-overview'),
  recomputeRollup: () => post<{ priceStats: number; featureImpacts: number }>('/api/stats/rollup'),
  estimate: (body: Record<string, unknown>) => postJson<EstimateResult>('/api/estimate', body),
  catalogMakes: () => get<CatalogFacet[]>('/api/catalog/makes'),
  catalogModels: (make: string) => get<CatalogFacet[]>(`/api/catalog/models?make=${encodeURIComponent(make)}`),
  searchLots: (params: Record<string, string | number>) => {
    const qs = new URLSearchParams(
      Object.entries(params)
        .filter(([, v]) => v !== '' && v != null)
        .map(([k, v]) => [k, String(v)]),
    ).toString()
    return get<LotSearchResult>('/api/lots?' + qs)
  },
}
