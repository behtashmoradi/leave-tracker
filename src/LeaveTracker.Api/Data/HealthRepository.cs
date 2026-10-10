using Dapper;

namespace LeaveTracker.Api.Data;

public sealed class HealthRepository(ISqlConnectionFactory connectionFactory, ILogger<HealthRepository> logger)
{
    public async Task<bool> CanConnectAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = connectionFactory.Create();
            var result = await connection.ExecuteScalarAsync<int>(
                new CommandDefinition("SELECT 1", cancellationToken: cancellationToken));
            return result == 1;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Database health check failed");
            return false;
        }
    }
}
