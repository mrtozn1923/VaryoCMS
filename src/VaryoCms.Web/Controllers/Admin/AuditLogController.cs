using VaryoCms.Application.Interfaces;
using VaryoCms.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace VaryoCms.Web.Controllers.Admin;

[Authorize(Roles = "TenantAdmin,SystemAdmin")]
[Route("admin/logs")]
public class AuditLogController : Controller
{
    private readonly IAuditLogService _audit;

    public AuditLogController(IAuditLogService audit) => _audit = audit;

    [HttpGet("")]
    public async Task<IActionResult> Index(
        string? filterAction, string? entityType, int? contentTypeId,
        DateTime? dateFrom, DateTime? dateTo,
        int page = 1, CancellationToken ct = default)
    {
        var result = await _audit.GetPagedAsync(page, 30, filterAction, entityType, contentTypeId, dateFrom, dateTo, ct);
        if (!result.IsSuccess) return View(new AuditLogListViewModel());

        return View(new AuditLogListViewModel
        {
            Items      = result.Value!.Items,
            TotalCount = result.Value.Total,
            Page       = result.Value.Page,
            PageSize   = result.Value.PageSize,
            FilterAction      = filterAction,
            FilterEntityType  = entityType,
            FilterContentTypeId = contentTypeId,
            FilterDateFrom    = dateFrom,
            FilterDateTo      = dateTo,
        });
    }
}
