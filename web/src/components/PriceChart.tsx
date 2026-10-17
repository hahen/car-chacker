import type { ModelYearPrice } from '../types'

interface Props {
  points: ModelYearPrice[]
}

// Dependency-free SVG line chart: P25–P75 band with the P50 median line, by model year.
export function PriceChart({ points }: Props) {
  const pts = points.filter((p) => p.p50 != null)
  if (pts.length === 0) {
    return <div className="chart-empty">No price data for this selection.</div>
  }

  const W = 640
  const H = 300
  const padL = 56
  const padR = 16
  const padT = 16
  const padB = 36

  const lows = pts.map((p) => p.p25 ?? p.p50!)
  const highs = pts.map((p) => p.p75 ?? p.p50!)
  const minY = Math.min(...lows)
  const maxY = Math.max(...highs)
  const yPad = (maxY - minY) * 0.1 || 1000
  const y0 = Math.max(0, minY - yPad)
  const y1 = maxY + yPad

  const x = (i: number) =>
    pts.length === 1
      ? (padL + (W - padR)) / 2
      : padL + (i * (W - padL - padR)) / (pts.length - 1)
  const y = (v: number) => padT + ((y1 - v) * (H - padT - padB)) / (y1 - y0)

  const bandTop = pts.map((p, i) => `${x(i)},${y(p.p75 ?? p.p50!)}`).join(' ')
  const bandBottom = pts
    .map((p, i) => `${x(i)},${y(p.p25 ?? p.p50!)}`)
    .reverse()
    .join(' ')
  const bandPath = `${bandTop} ${bandBottom}`
  const medianLine = pts.map((p, i) => `${x(i)},${y(p.p50!)}`).join(' ')

  const ticks = 4
  const yTicks = Array.from({ length: ticks + 1 }, (_, i) => y0 + ((y1 - y0) * i) / ticks)
  const fmt = (v: number) => '$' + Math.round(v / 1000) + 'k'

  return (
    <svg className="chart" viewBox={`0 0 ${W} ${H}`} role="img" aria-label="Price by model year">
      {yTicks.map((t, i) => (
        <g key={i}>
          <line x1={padL} y1={y(t)} x2={W - padR} y2={y(t)} className="grid" />
          <text x={padL - 8} y={y(t) + 4} className="axis" textAnchor="end">
            {fmt(t)}
          </text>
        </g>
      ))}
      <polygon points={bandPath} className="band" />
      <polyline points={medianLine} className="median" fill="none" />
      {pts.map((p, i) => (
        <g key={p.year}>
          <circle cx={x(i)} cy={y(p.p50!)} r={4} className="dot" />
          <text x={x(i)} y={H - padB + 20} className="axis" textAnchor="middle">
            {p.year}
          </text>
        </g>
      ))}
    </svg>
  )
}
