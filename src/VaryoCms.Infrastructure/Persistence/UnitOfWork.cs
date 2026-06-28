using System.Data;
using VaryoCms.Application.Interfaces;

namespace VaryoCms.Infrastructure.Persistence;

// Scoped. Owns a single open connection + transaction for the duration of a write operation.
// Repositories participating in a transaction read Connection/Transaction from here.
public class UnitOfWork : IUnitOfWork
{
    private readonly IDbConnectionFactory _factory;
    private IDbConnection? _connection;
    private IDbTransaction? _transaction;

    public UnitOfWork(IDbConnectionFactory factory) => _factory = factory;

    public IDbConnection? Connection => _connection;
    public IDbTransaction? Transaction => _transaction;

    public async Task BeginAsync(CancellationToken ct = default)
    {
        if (_transaction is not null)
            throw new InvalidOperationException("A transaction is already in progress.");

        _connection = _factory.CreateConnection();
        if (_connection is System.Data.Common.DbConnection dbConn)
            await dbConn.OpenAsync(ct);
        else
            _connection.Open();

        _transaction = _connection.BeginTransaction();
    }

    public Task CommitAsync(CancellationToken ct = default)
    {
        if (_transaction is null)
            throw new InvalidOperationException("No transaction to commit.");

        _transaction.Commit();
        Cleanup();
        return Task.CompletedTask;
    }

    public Task RollbackAsync(CancellationToken ct = default)
    {
        if (_transaction is null)
            throw new InvalidOperationException("No transaction to roll back.");

        _transaction.Rollback();
        Cleanup();
        return Task.CompletedTask;
    }

    private void Cleanup()
    {
        _transaction?.Dispose();
        _transaction = null;
        _connection?.Dispose();
        _connection = null;
    }

    public ValueTask DisposeAsync()
    {
        // Roll back any uncommitted work on scope disposal.
        if (_transaction is not null)
        {
            _transaction.Rollback();
            Cleanup();
        }
        return ValueTask.CompletedTask;
    }
}
