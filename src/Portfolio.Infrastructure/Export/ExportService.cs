using System.Globalization;
using ClosedXML.Excel;
using CsvHelper;
using Portfolio.Application.Export;
using Portfolio.Application.Holdings;

namespace Portfolio.Infrastructure.Export;

public class ExportService(IHoldingsService holdingsService) : IExportService
{
    public async Task<byte[]?> ExportHoldingsToCsvAsync(Guid userId, Guid portfolioId, CancellationToken cancellationToken)
    {
        var holdings = await holdingsService.GetHoldingsAsync(userId, portfolioId, cancellationToken);
        if (holdings is null)
        {
            return null;
        }

        using var stream = new MemoryStream();
        await using (var writer = new StreamWriter(stream, leaveOpen: true))
        await using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
        {
            csv.WriteRecords(holdings);
        }

        return stream.ToArray();
    }

    public async Task<byte[]?> ExportHoldingsToXlsxAsync(Guid userId, Guid portfolioId, CancellationToken cancellationToken)
    {
        var holdings = await holdingsService.GetHoldingsAsync(userId, portfolioId, cancellationToken);
        if (holdings is null)
        {
            return null;
        }

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Holdings");
        worksheet.Cell(1, 1).InsertTable(holdings, "Holdings", true);
        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
