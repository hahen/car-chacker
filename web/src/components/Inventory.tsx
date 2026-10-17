import { useEffect, useState } from 'react'
import { api } from '../api'
import type { CatalogFacet, LotListItem } from '../types'

const money = (v: number | null) =>
  v == null ? '—' : v.toLocaleString('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0 })
const fmtDate = (s: string | null) =>
  s ? new Date(s).toLocaleDateString(undefined, { month: 'short', day: 'numeric', year: 'numeric' }) : null

const TITLE_GROUPS = ['', 'Clean', 'Salvage', 'Rebuilt', 'Junk', 'Nonrepairable']
const DAMAGE_GROUPS = ['', 'Front', 'Rear', 'Side', 'AllOver', 'Water', 'Fire', 'Hail', 'Mechanical', 'Normal']
const PAGE_SIZE = 24

export function Inventory() {
  const [makes, setMakes] = useState<CatalogFacet[]>([])
  const [models, setModels] = useState<CatalogFacet[]>([])
  const [make, setMake] = useState('')
  const [model, setModel] = useState('')
  const [source, setSource] = useState('')
  const [status, setStatus] = useState('upcoming')
  const [titleGroup, setTitleGroup] = useState('')
  const [damageGroup, setDamageGroup] = useState('')
  const [sort, setSort] = useState('auction')
  const [page, setPage] = useState(1)

  const [items, setItems] = useState<LotListItem[]>([])
  const [total, setTotal] = useState(0)
  const [busy, setBusy] = useState(false)
  const [err, setErr] = useState<string | null>(null)

  useEffect(() => {
    api.catalogMakes().then(setMakes).catch((e) => setErr(String(e)))
  }, [])

  useEffect(() => {
    if (!make) { setModels([]); return }
    api.catalogModels(make).then(setModels).catch(() => setModels([]))
  }, [make])

  useEffect(() => {
    setBusy(true)
    setErr(null)
    api
      .searchLots({ make, model, source, status, titleGroup, damageGroup, sort, page, pageSize: PAGE_SIZE })
      .then((r) => { setItems(r.items); setTotal(r.total) })
      .catch((e) => setErr(String(e)))
      .finally(() => setBusy(false))
  }, [make, model, source, status, titleGroup, damageGroup, sort, page])

  // Resetting any filter goes back to page 1.
  function reset<T>(setter: (v: T) => void) {
    return (v: T) => { setter(v); setPage(1) }
  }

  const pages = Math.max(1, Math.ceil(total / PAGE_SIZE))

  return (
    <section className="inventory">
      <div className="sold-head">
        <h2>Inventory <span className="muted-note">({total} lots)</span></h2>
      </div>

      <div className="filters">
        <label>Make
          <select value={make} onChange={(e) => { reset(setMake)(e.target.value); setModel('') }}>
            <option value="">Any</option>
            {makes.map((m) => <option key={m.name} value={m.name}>{m.name} ({m.count})</option>)}
          </select>
        </label>
        <label>Model
          <select value={model} onChange={(e) => reset(setModel)(e.target.value)} disabled={!make}>
            <option value="">Any</option>
            {models.map((m) => <option key={m.name} value={m.name}>{m.name} ({m.count})</option>)}
          </select>
        </label>
        <label>Source
          <select value={source} onChange={(e) => reset(setSource)(e.target.value)}>
            <option value="">Any</option><option value="Copart">Copart</option><option value="Iaai">IAAI</option>
          </select>
        </label>
        <label>Status
          <select value={status} onChange={(e) => reset(setStatus)(e.target.value)}>
            <option value="">All</option><option value="upcoming">Upcoming</option><option value="sold">Sold</option>
          </select>
        </label>
        <label>Title
          <select value={titleGroup} onChange={(e) => reset(setTitleGroup)(e.target.value)}>
            {TITLE_GROUPS.map((g) => <option key={g} value={g}>{g || 'Any'}</option>)}
          </select>
        </label>
        <label>Damage
          <select value={damageGroup} onChange={(e) => reset(setDamageGroup)(e.target.value)}>
            {DAMAGE_GROUPS.map((g) => <option key={g} value={g}>{g || 'Any'}</option>)}
          </select>
        </label>
        <label>Sort
          <select value={sort} onChange={(e) => reset(setSort)(e.target.value)}>
            <option value="auction">Auction date</option>
            <option value="price">Price</option>
            <option value="year">Year</option>
            <option value="recent">Recently added</option>
          </select>
        </label>
      </div>

      {err && <div className="banner error">{err}</div>}
      {!busy && items.length === 0 && (
        <p className="muted-note">
          No lots match. Run the collectors (<code>scrape iaai</code> / <code>scrape copart</code>) to populate inventory.
        </p>
      )}

      <div className="lot-grid">
        {items.map((l) => (
          <div className="lot-card" key={l.id}>
            <div className="lot-photo">
              {l.thumbnailUrl ? <img src={l.thumbnailUrl} alt="" loading="lazy" /> : <div className="no-photo">no photo</div>}
              <span className={`src-badge ${l.source.toLowerCase()}`}>{l.source}</span>
            </div>
            <div className="lot-body">
              <div className="lot-title">{[l.year, l.make, l.model, l.trim].filter(Boolean).join(' ')}</div>
              <div className="lot-price">
                {money(l.salePrice ?? l.currentBid ?? l.buyNowPrice)}
                {l.isSold ? ' sold' : ''}
              </div>
              <div className="lot-meta">
                {l.damageCode && <span className="tag">{l.damageCode}</span>}
                {l.titleCode && <span className="tag">{l.titleCode}</span>}
                {l.odometer != null && <span className="tag">{l.odometer.toLocaleString()} mi</span>}
              </div>
              <div className="lot-vin">VIN {l.vin ?? '—'}{!l.vinFull && l.vin ? ' (partial)' : ''}</div>
              <div className="lot-foot">
                <span>{[l.locationCity, l.locationState].filter(Boolean).join(', ') || '—'}</span>
                <span>{fmtDate(l.auctionDateUtc) ?? l.status}</span>
              </div>
              {l.lotUrl && <a className="lot-link" href={l.lotUrl} target="_blank" rel="noreferrer">View lot ↗</a>}
            </div>
          </div>
        ))}
      </div>

      {pages > 1 && (
        <div className="pager">
          <button className="ghost" disabled={page <= 1} onClick={() => setPage((p) => p - 1)}>← Prev</button>
          <span>Page {page} / {pages}</span>
          <button className="ghost" disabled={page >= pages} onClick={() => setPage((p) => p + 1)}>Next →</button>
        </div>
      )}
    </section>
  )
}
