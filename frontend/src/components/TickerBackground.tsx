interface TickerEntry {
  symbol: string
  price: number
  change: number
}

// Purely decorative background texture — not real market data.
const ROW_1: TickerEntry[] = [
  { symbol: 'AAPL', price: 233.45, change: 1.2 },
  { symbol: 'MSFT', price: 441.02, change: -0.4 },
  { symbol: 'NVDA', price: 128.77, change: 3.1 },
  { symbol: 'GOOGL', price: 178.3, change: 0.6 },
  { symbol: 'AMZN', price: 201.15, change: -1.1 },
  { symbol: 'TSLA', price: 264.88, change: 4.7 },
  { symbol: 'META', price: 592.6, change: -0.2 },
  { symbol: 'BRK.B', price: 452.11, change: 0.3 },
]

const ROW_2: TickerEntry[] = [
  { symbol: 'JPM', price: 221.4, change: 0.8 },
  { symbol: 'V', price: 312.9, change: -0.3 },
  { symbol: 'XOM', price: 118.2, change: 1.5 },
  { symbol: 'SPY', price: 587.6, change: 0.4 },
  { symbol: 'QQQ', price: 502.3, change: 0.9 },
  { symbol: 'BTC', price: 71234.0, change: -2.3 },
  { symbol: 'ETH', price: 3891.5, change: 1.8 },
  { symbol: 'DIS', price: 96.4, change: -0.6 },
]

const ROW_3: TickerEntry[] = [
  { symbol: 'NFLX', price: 812.0, change: 2.4 },
  { symbol: 'AMD', price: 152.7, change: -1.8 },
  { symbol: 'KO', price: 68.1, change: 0.1 },
  { symbol: 'PFE', price: 27.9, change: -0.5 },
  { symbol: 'BA', price: 189.3, change: 1.1 },
  { symbol: 'WMT', price: 84.6, change: 0.5 },
  { symbol: 'IWM', price: 224.8, change: -0.9 },
  { symbol: 'DIA', price: 421.7, change: 0.2 },
]

function TickerRow({ entries, reverse, duration }: { entries: TickerEntry[]; reverse?: boolean; duration: number }) {
  const doubled = [...entries, ...entries]
  return (
    <div
      className="ticker-row"
      style={{ animationDuration: `${duration}s`, animationDirection: reverse ? 'reverse' : 'normal' }}
    >
      {doubled.map((entry, index) => (
        <span key={`${entry.symbol}-${index}`} className="ticker-item">
          <span className="ticker-symbol">{entry.symbol}</span>
          <span>{entry.price.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</span>
          <span className={entry.change >= 0 ? 'positive' : 'negative'}>
            {entry.change >= 0 ? '▲' : '▼'} {Math.abs(entry.change).toFixed(1)}%
          </span>
        </span>
      ))}
    </div>
  )
}

export function TickerBackground() {
  return (
    <div className="ticker-bg" aria-hidden="true">
      <TickerRow entries={ROW_1} duration={62} />
      <TickerRow entries={ROW_2} reverse duration={78} />
      <TickerRow entries={ROW_3} duration={70} />
    </div>
  )
}
