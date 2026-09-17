namespace Portfolio.Application.Analytics;

public record PerformersResponse(IReadOnlyList<PerformerEntry> Top, IReadOnlyList<PerformerEntry> Bottom);
