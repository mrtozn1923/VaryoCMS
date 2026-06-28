namespace VaryoCms.Domain.Interfaces.Repositories;

public record DashboardCounts(
    int ContentTypes,
    int ContentItems,
    int MediaAssets,
    int Users,
    int Languages);

public interface IDashboardRepository
{
    Task<DashboardCounts> GetCountsAsync(CancellationToken ct = default);
}
