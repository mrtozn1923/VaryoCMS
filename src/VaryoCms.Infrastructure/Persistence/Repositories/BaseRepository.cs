using System.Data;

namespace VaryoCms.Infrastructure.Persistence.Repositories;

// Shared plumbing for Dapper repositories. Read paths open a short-lived connection;
// write paths inside a transaction should use the UnitOfWork's connection/transaction.
public abstract class BaseRepository
{
    protected readonly IDbConnectionFactory ConnectionFactory;

    protected BaseRepository(IDbConnectionFactory connectionFactory)
        => ConnectionFactory = connectionFactory;

    protected IDbConnection CreateConnection() => ConnectionFactory.CreateConnection();
}
