using Dapper;

namespace LeaveTracker.Api.Data;

public sealed class LeaveRequestRepository(ISqlConnectionFactory connectionFactory)
{
    private const string Columns = "Id, EmployeeId, StartDate, EndDate, LeaveType, Status, Reason, CreatedAt";

    public async Task<bool> EmployeeExistsAsync(int employeeId, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.Create();
        return await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
            "SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.Employees WHERE Id = @EmployeeId) THEN 1 ELSE 0 END",
            new { EmployeeId = employeeId },
            cancellationToken: cancellationToken));
    }

    public async Task<LeaveRequest> CreateAsync(
        int employeeId, DateOnly startDate, DateOnly endDate, string leaveType, string? reason,
        CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.Create();
        return await connection.QuerySingleAsync<LeaveRequest>(new CommandDefinition(
            """
            INSERT INTO dbo.LeaveRequests (EmployeeId, StartDate, EndDate, LeaveType, Reason)
            OUTPUT INSERTED.Id, INSERTED.EmployeeId, INSERTED.StartDate, INSERTED.EndDate,
                   INSERTED.LeaveType, INSERTED.Status, INSERTED.Reason, INSERTED.CreatedAt
            VALUES (@EmployeeId, @StartDate, @EndDate, @LeaveType, @Reason)
            """,
            new { EmployeeId = employeeId, StartDate = startDate, EndDate = endDate, LeaveType = leaveType, Reason = reason },
            cancellationToken: cancellationToken));
    }

    public async Task<LeaveRequest?> GetAsync(int employeeId, int id, CancellationToken cancellationToken)
    {
        await using var connection = connectionFactory.Create();
        return await connection.QuerySingleOrDefaultAsync<LeaveRequest>(new CommandDefinition(
            $"SELECT {Columns} FROM dbo.LeaveRequests WHERE Id = @Id AND EmployeeId = @EmployeeId",
            new { Id = id, EmployeeId = employeeId },
            cancellationToken: cancellationToken));
    }
}
