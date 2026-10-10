namespace LeaveTracker.Api.Data;

// Settable properties rather than a positional record: Dapper's constructor matching
// doesn't apply type handlers, so DATE -> DateOnly only works via property mapping.
public sealed class LeaveRequest
{
    public int Id { get; init; }
    public int EmployeeId { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
    public string LeaveType { get; init; } = "";
    public string Status { get; init; } = "";
    public string? Reason { get; init; }
    public DateTime CreatedAt { get; init; }
}
