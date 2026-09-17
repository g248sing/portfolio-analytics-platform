import { useQuery } from '@tanstack/react-query'
import { getTicker } from '../api/tickerApi'
import type { TickerEntryDto } from '../api/types'

const ROW_DURATIONS = [62, 78, 70]

function splitIntoRows(entries: TickerEntryDto[], rowCount: number): TickerEntryDto[][] {
  const rows: TickerEntryDto[][] = Array.from({ length: rowCount }, () => [])
  entries.forEach((entry, index) => rows[index % rowCount].push(entry))
  // A thin ticker (few real symbols tracked so far) still fills every row —
  // repetition here is fine since it's genuinely real data, not fabricated.
  return rows.map((row) => (row.length > 0 ? row : entries))
}

function TickerRow({ entries, reverse, duration }: { entries: TickerEntryDto[]; reverse?: boolean; duration: number }) {
  const doubled = [...entries, ...entries]
  return (
    <div
      className="ticker-row"
      style={{ animationDuration: `${duration}s`, animationDirection: reverse ? 'reverse' : 'normal' }}
    >
      {doubled.map((entry, index) => (
        <span key={`${entry.symbol}-${index}`} className="ticker-item">
          <span className="ticker-symbol">{entry.symbol}</span>
          <span>{entry.latestClose.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</span>
          {entry.changePercent !== null && (
            <span className={entry.changePercent >= 0 ? 'positive' : 'negative'}>
              {entry.changePercent >= 0 ? '▲' : '▼'} {Math.abs(entry.changePercent).toFixed(1)}%
            </span>
          )}
        </span>
      ))}
    </div>
  )
}

export function TickerBackground() {
  const { data } = useQuery({
    queryKey: ['ticker'],
    queryFn: () => getTicker(24),
    refetchInterval: 5 * 60 * 1000,
    staleTime: 60 * 1000,
  })

  if (!data || data.length === 0) {
    return null
  }

  const rows = splitIntoRows(data, ROW_DURATIONS.length)

  return (
    <div className="ticker-bg" aria-hidden="true">
      {rows.map((row, index) => (
        <TickerRow key={index} entries={row} reverse={index === 1} duration={ROW_DURATIONS[index]} />
      ))}
    </div>
  )
}
