using Portfolio.Domain.Enums;

namespace Portfolio.Domain.Entities;

/// <summary>
/// Audit record for one run of the background price-refresh job.
/// </summary>
public class PriceRefreshLog
{
    public Guid Id { get; set; }

    public DateTimeOffset StartedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public JobRunStatus Status { get; set; } = JobRunStatus.Running;

    public int SymbolsProcessed { get; set; }

    public int SymbolsFailed { get; set; }

    public string? ErrorDetails { get; set; }
}
