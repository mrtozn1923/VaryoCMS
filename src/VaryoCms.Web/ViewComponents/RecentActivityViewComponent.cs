using VaryoCms.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace VaryoCms.Web.ViewComponents;

public class RecentActivityViewComponent : ViewComponent
{
    private readonly IAuditLogService _audit;

    public RecentActivityViewComponent(IAuditLogService audit) => _audit = audit;

    public async Task<IViewComponentResult> InvokeAsync(int take = 10)
    {
        var result = await _audit.GetRecentAsync(take);
        return View(result.IsSuccess ? result.Value! : []);
    }
}
