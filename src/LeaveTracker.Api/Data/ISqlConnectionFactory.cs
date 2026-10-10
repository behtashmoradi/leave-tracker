using System.Data.Common;

namespace LeaveTracker.Api.Data;

public interface ISqlConnectionFactory
{
    string ConnectionString { get; }

    DbConnection Create();
}
