import { apiClient } from './client'

async function downloadFile(url: string, filename: string) {
  const response = await apiClient.get(url, { responseType: 'blob' })
  const objectUrl = URL.createObjectURL(response.data as Blob)
  const link = document.createElement('a')
  link.href = objectUrl
  link.download = filename
  link.click()
  URL.revokeObjectURL(objectUrl)
}

export function downloadHoldingsCsv(portfolioId: string) {
  return downloadFile(`/portfolios/${portfolioId}/export/csv`, 'holdings.csv')
}

export function downloadHoldingsXlsx(portfolioId: string) {
  return downloadFile(`/portfolios/${portfolioId}/export/xlsx`, 'holdings.xlsx')
}
