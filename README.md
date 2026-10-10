# LeaveTracker

ASP.NET Core (.NET 10) Web API for tracking employee leave. Uses controllers, Dapper and SQL Server; the schema is managed by DbUp.

## Layout

| Path | What |
| --- | --- |
| `src/LeaveTracker.Api` | Web API (controllers, Dapper repositories, DbUp migrator) |
| `db/migrations` | Numbered SQL migration scripts, embedded into the API and applied on startup |
| `tests/LeaveTracker.Api.Tests` | xUnit integration tests against a throwaway SQL Server (Testcontainers) |

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Docker (Docker Desktop on Windows/macOS)

## Run locally

1. Start SQL Server in Docker (container name `sql-dev`, port 1433):

   ```bash
   docker run -d --name sql-dev -p 1433:1433 \
     -e ACCEPT_EULA=Y -e MSSQL_SA_PASSWORD='<YourStrong!Passw0rd>' \
     mcr.microsoft.com/mssql/server:2022-latest
   ```

   If the container already exists: `docker start sql-dev`.

2. Store the connection string in user-secrets. Never put it in `appsettings.json`.

   ```bash
   dotnet user-secrets set "ConnectionStrings:LeaveTracker" \
     "Server=localhost,1433;Database=LeaveTracker;User Id=sa;Password=<YourStrong!Passw0rd>;TrustServerCertificate=True" \
     --project src/LeaveTracker.Api
   ```

3. Run the API. On startup, DbUp creates the `LeaveTracker` database if it is missing and applies any new migrations:

   ```bash
   dotnet run --project src/LeaveTracker.Api
   ```

4. Check it:

   ```bash
   curl http://localhost:5182/health
   # {"status":"Healthy"}   (503 {"status":"Unhealthy"} if the database is unreachable)
   ```

## Build and test

```bash
dotnet build
dotnet test
```

The tests start their own SQL Server container through Testcontainers, so Docker must be running. They don't use `sql-dev`.

## Adding a migration

Add the next numbered script to `db/migrations`, for example `0003_add_something.sql`. It is embedded into the API automatically, and DbUp applies it on the next startup. Never edit a script that has already run; DbUp tracks applied scripts in `dbo.SchemaVersions`.
