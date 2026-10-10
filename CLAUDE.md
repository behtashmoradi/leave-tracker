Build and test
Build: dotnet build
Test: dotnet test
DB runs in Docker (sql-dev, localhost,1433). Connection string comes from user-secrets, never appsettings.json.
Conventions
.NET 10, ASP.NET Core controllers (not minimal APIs), Dapper (no EF Core)
SQL lives in repository classes; parameterised queries only
Schema changes = new numbered script in /db/migrations, applied by DbUp
Every new endpoint gets an integration test
Git workflow
Never commit to main. Branch per task: feature/<issue-number>-<slug>
Small commits with clear messages; open PRs with gh pr create, body links the issue
Never merge PRs. Never force-push. A human merges.