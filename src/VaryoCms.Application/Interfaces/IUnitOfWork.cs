namespace VaryoCms.Application.Interfaces;

// Transaction boundary for multi-repository write operations.
// Read paths open their own short-lived connection via IDbConnectionFactory (Infrastructure).
public interface IUnitOfWork : IAsyncDisposable
{
    Task BeginAsync(CancellationToken ct = default);
    Task CommitAsync(CancellationToken ct = default);
    Task RollbackAsync(CancellationToken ct = default);
}
