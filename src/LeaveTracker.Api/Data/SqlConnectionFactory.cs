using System.Data.Common;
using Microsoft.Data.SqlClient;

namespace LeaveTracker.Api.Data;

public sealed class SqlConnectionFactory(IConfiguration configuration) : ISqlConnectionFactory
{
    public const string ConnectionStringName = "LeaveTracker";

    public string ConnectionString { get; } =
        configuration.GetConnectionString(ConnectionStringName)
        ?? throw new InvalidOperationException(
            $"Connection string '{ConnectionStringName}' is not configured. " +
            $"Set it with: dotnet user-secrets set \"ConnectionStrings:{ConnectionStringName}\" \"<connection string>\"");

    public DbConnection Create() => new SqlConnection(ConnectionString);
}
