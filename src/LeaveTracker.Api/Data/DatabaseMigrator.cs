using DbUp;

namespace LeaveTracker.Api.Data;

public static class DatabaseMigrator
{
    // Scripts in db/migrations are embedded with this prefix (see LeaveTracker.Api.csproj).
    private const string ScriptPrefix = "Migrations.";

    public static void Migrate(string connectionString, ILogger logger)
    {
        EnsureDatabase.For.SqlDatabase(connectionString);

        var upgrader = DeployChanges.To
            .SqlDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(typeof(DatabaseMigrator).Assembly, name => name.StartsWith(ScriptPrefix))
            .WithTransactionPerScript()
            .LogToConsole()
            .Build();

        var result = upgrader.PerformUpgrade();
        if (!result.Successful)
        {
            throw new InvalidOperationException(
                $"Database migration failed on script '{result.ErrorScript?.Name}'.", result.Error);
        }

        logger.LogInformation("Database migrations applied ({Count} new script(s))", result.Scripts.Count());
    }
}
