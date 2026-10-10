namespace LeaveTracker.Api.Data;

// Must match CK_LeaveRequests_LeaveType (db/migrations/0003_add_leave_type_check.sql).
public static class LeaveTypes
{
    public static readonly IReadOnlyList<string> All = ["Annual", "Sick", "Unpaid"];

    public static string? Normalize(string? value) =>
        All.FirstOrDefault(t => string.Equals(t, value?.Trim(), StringComparison.OrdinalIgnoreCase));
}
