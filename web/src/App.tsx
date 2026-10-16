import { useEffect, useState } from 'react'
import { api } from './api'
import { PriceChart } from './components/PriceChart'
import type { MakeSummary, MarketOverview, ModelPriceSeries } from './types'

const money = (v: number | null | undefined) =>
  v == null ? '—' : v.toLocaleString('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0 })

export default function App() {
  const [overview, setOverview] = useState<MarketOverview | null>(null)
  const [makes, setMakes] = useState<MakeSummary[]>([])
  const [makeList, setMakeList] = useState<string[]>([])
  const [selectedMake, setSelectedMake] = useState('')
  const [models, setModels] = useState<string[]>([])
  const [selectedModel, setSelectedModel] = useState('')
  const [series, setSeries] = useState<ModelPriceSeries | null>(null)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)

  async function loadSummary() {
    setError(null)
    try {
      const [ov, ms, mk] = await Promise.all([api.overview(), api.makesSummary(), api.makes()])
      setOverview(ov)
      setMakes(ms)
      setMakeList(mk)
      if (mk.length && !selectedMake) setSelectedMake(mk[0])
    } catch (e) {
      setError(String(e))
    }
  }

  useEffect(() => {
    loadSummary()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  useEffect(() => {
    if (!selectedMake) return
    api
      .models(selectedMake)
      .then((m) => {
        setModels(m)
        setSelectedModel(m[0] ?? '')
      })
      .catch((e) => setError(String(e)))
  }, [selectedMake])

  useEffect(() => {
    if (!selectedMake || !selectedModel) {
      setSeries(null)
      return
    }
    api.series(selectedMake, selectedModel).then(setSeries).catch((e) => setError(String(e)))
  }, [selectedMake, selectedModel])

  async function importSample() {
    setBusy(true)
    setError(null)
    try {
      await api.importSample()
      await loadSummary()
    } catch (e) {
      setError(String(e))
    } finally {
      setBusy(false)
    }
  }

  const empty = overview && overview.indexRows === 0

  return (
    <div className="app">
      <header className="topbar">
        <div>
          <h1>CarPrices</h1>
          <p className="sub">Auction price intelligence · Phase 0 (VinCheck aggregate index)</p>
        </div>
        <div className="actions">
          <button onClick={importSample} disabled={busy}>
            {busy ? 'Importing…' : 'Load sample data'}
          </button>
          <button className="ghost" onClick={loadSummary} disabled={busy}>
            Refresh
          </button>
        </div>
      </header>

      {error && <div className="banner error">{error}</div>}
      {empty && (
        <div className="banner">
          No data yet. Click <strong>Load sample data</strong> to import the bundled sample index.
        </div>
      )}

      <section className="cards">
        <Card label="Makes" value={overview?.makes ?? '—'} />
        <Card label="Models" value={overview?.models ?? '—'} />
        <Card label="Index rows" value={overview?.indexRows ?? '—'} />
        <Card label="Observations" value={overview?.totalObservations?.toLocaleString() ?? '—'} />
        <Card label="Median price" value={money(overview?.medianPrice)} />
        <Card
          label="Year range"
          value={overview?.minYear ? `${overview.minYear}–${overview.maxYear}` : '—'}
        />
      </section>

      <div className="grid">
        <section className="panel">
          <h2>Makes</h2>
          <table>
            <thead>
              <tr>
                <th>Make</th>
                <th className="num">Models</th>
                <th className="num">Obs.</th>
                <th className="num">Median</th>
              </tr>
            </thead>
            <tbody>
              {makes.map((m) => (
                <tr
                  key={m.make}
                  className={m.make === selectedMake ? 'active' : ''}
                  onClick={() => setSelectedMake(m.make)}
                >
                  <td>{m.make}</td>
                  <td className="num">{m.models}</td>
                  <td className="num">{m.observations.toLocaleString()}</td>
                  <td className="num">{money(m.medianPrice)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </section>

        <section className="panel">
          <h2>Price by model year</h2>
          <div className="selectors">
            <label>
              Make
              <select value={selectedMake} onChange={(e) => setSelectedMake(e.target.value)}>
                {makeList.map((m) => (
                  <option key={m} value={m}>
                    {m}
                  </option>
                ))}
              </select>
            </label>
            <label>
              Model
              <select value={selectedModel} onChange={(e) => setSelectedModel(e.target.value)}>
                {models.map((m) => (
                  <option key={m} value={m}>
                    {m}
                  </option>
                ))}
              </select>
            </label>
          </div>

          <PriceChart points={series?.points ?? []} />
          <p className="legend">
            <span className="swatch band" /> 25th–75th percentile band
            <span className="swatch median" /> median (50th)
          </p>

          {series && series.points.length > 0 && (
            <table className="compact">
              <thead>
                <tr>
                  <th>Year</th>
                  <th className="num">N</th>
                  <th className="num">P25</th>
                  <th className="num">Median</th>
                  <th className="num">P75</th>
                </tr>
              </thead>
              <tbody>
                {series.points.map((p) => (
                  <tr key={p.year}>
                    <td>{p.year}</td>
                    <td className="num">{p.sampleSize}</td>
                    <td className="num">{money(p.p25)}</td>
                    <td className="num">{money(p.p50)}</td>
                    <td className="num">{money(p.p75)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </section>
      </div>

      <footer className="foot">
        Data source: VinCheck aggregate index (CC BY 4.0). Sample data is illustrative only.
      </footer>
    </div>
  )
}

function Card({ label, value }: { label: string; value: string | number }) {
  return (
    <div className="card">
      <div className="card-label">{label}</div>
      <div className="card-value">{value}</div>
    </div>
  )
}
