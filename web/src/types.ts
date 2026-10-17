export interface MarketOverview {
  makes: number
  models: number
  indexRows: number
  totalObservations: number
  medianPrice: number | null
  minYear: number | null
  maxYear: number | null
}

export interface MakeSummary {
  make: string
  models: number
  observations: number
  medianPrice: number | null
}

export interface ModelYearPrice {
  year: number
  sampleSize: number
  p25: number | null
  p50: number | null
  p75: number | null
}

export interface ModelPriceSeries {
  make: string
  model: string
  points: ModelYearPrice[]
}

export interface ImportResult {
  rows: number
  inserted: number
  updated: number
  skipped: number
}

export interface SoldOverview {
  soldLots: number
  medianSoldPrice: number | null
  minYear: number | null
  maxYear: number | null
  priceStatRows: number
  featureImpactRows: number
  lastComputedUtc: string | null
}

export interface EstimateResult {
  comparables: number
  median: number | null
  p25: number | null
  p75: number | null
  mean: number | null
  suggested: number | null
  mileageAdjusted: number | null
  basis: string
}

export interface CatalogFacet {
  name: string
  count: number
}

export interface LotListItem {
  id: number
  source: string
  lotNumber: string
  lotUrl: string | null
  make: string | null
  model: string | null
  trim: string | null
  year: number | null
  vin: string | null
  vinFull: boolean
  currentBid: number | null
  buyNowPrice: number | null
  salePrice: number | null
  isSold: boolean
  status: string
  titleCode: string | null
  damageCode: string | null
  odometer: number | null
  locationCity: string | null
  locationState: string | null
  auctionDateUtc: string | null
  thumbnailUrl: string | null
  features: string[]
}

export interface LotSearchResult {
  total: number
  page: number
  pageSize: number
  items: LotListItem[]
}
