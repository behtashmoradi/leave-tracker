using LeaveTracker.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.MsSql;

namespace LeaveTracker.Api.Tests;

public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _sqlContainer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Point the app at the throwaway container; DbUp creates the database and runs the migrations on startup.
        var connectionString = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(_sqlContainer.GetConnectionString())
        {
            InitialCatalog = "LeaveTracker"
        }.ConnectionString;

        builder.UseSetting($"ConnectionStrings:{SqlConnectionFactory.ConnectionStringName}", connectionString);
    }

    public Task InitializeAsync() => _sqlContainer.StartAsync();

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _sqlContainer.DisposeAsync();
        await base.DisposeAsync();
    }
}
