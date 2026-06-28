using System.Data;

namespace VaryoCms.Infrastructure.Persistence;

// Creates a new (closed) connection. Callers own it: `using var conn = factory.CreateConnection();`
public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}
