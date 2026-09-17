namespace Portfolio.Application.Analytics;

public record AllocationResponse(IReadOnlyList<AllocationSlice> BySector, IReadOnlyList<AllocationSlice> ByAssetClass);
