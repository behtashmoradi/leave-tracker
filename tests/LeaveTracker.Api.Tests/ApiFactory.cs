using Dapper;
using LeaveTracker.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;

namespace LeaveTracker.Api.Tests;

public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _sqlContainer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

    private string ConnectionString => new SqlConnectionStringBuilder(_sqlContainer.GetConnectionString())
    {
        InitialCatalog = "LeaveTracker"
    }.ConnectionString;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Point the app at the throwaway container; DbUp creates the database and runs the migrations on startup.
        builder.UseSetting($"ConnectionStrings:{SqlConnectionFactory.ConnectionStringName}", ConnectionString);
    }

    // Seeds an employee directly; there is no endpoint for creating employees yet.
    // Call after CreateClient() so the app has run its migrations.
    public async Task<int> CreateEmployeeAsync()
    {
        await using var connection = new SqlConnection(ConnectionString);
        return await connection.ExecuteScalarAsync<int>(
            """
            INSERT INTO dbo.Employees (FirstName, LastName, Email)
            OUTPUT INSERTED.Id
            VALUES (N'Test', N'Employee', @Email)
            """,
            new { Email = $"{Guid.NewGuid():N}@example.com" });
    }

    // There is no endpoint for changing a request's status yet.
    public async Task SetLeaveRequestStatusAsync(int leaveRequestId, string status)
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.ExecuteAsync(
            "UPDATE dbo.LeaveRequests SET Status = @Status WHERE Id = @Id",
            new { Id = leaveRequestId, Status = status });
    }

    public Task InitializeAsync() => _sqlContainer.StartAsync();

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _sqlContainer.DisposeAsync();
        await base.DisposeAsync();
    }
}
