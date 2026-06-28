using VaryoCms.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace VaryoCms.Web.ViewComponents;

public class ContentActivityViewComponent : ViewComponent
{
    private readonly IAuditLogService _audit;

    public ContentActivityViewComponent(IAuditLogService audit) => _audit = audit;

    public async Task<IViewComponentResult> InvokeAsync(int contentTypeId, int take = 10)
    {
        var result = await _audit.GetRecentForContentTypeAsync(contentTypeId, take);
        return View(result.IsSuccess ? result.Value! : []);
    }
}
