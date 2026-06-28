using VaryoCms.Application.DTOs.Navigation;
using VaryoCms.Application.Interfaces;
using VaryoCms.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace VaryoCms.Web.ViewComponents;

// Renders the content-type section of the sidebar as a collapsible tree.
public class NavMenuViewComponent : ViewComponent
{
    private readonly IPermissionService _permissions;

    public NavMenuViewComponent(IPermissionService permissions) => _permissions = permissions;

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var items = await _permissions.GetAccessibleContentTypesAsync();
        var roots = BuildTree(items);
        return View(roots);
    }

    private static List<NavMenuNodeViewModel> BuildTree(IReadOnlyList<AccessibleContentTypeDto> flat)
    {
        var byId = flat.ToDictionary(c => c.Id);
        var nodes = flat.ToDictionary(c => c.Id, c => new NavMenuNodeViewModel { ContentType = c });
        var roots = new List<NavMenuNodeViewModel>();

        foreach (var item in flat)
        {
            if (item.ParentId.HasValue && byId.ContainsKey(item.ParentId.Value))
                nodes[item.ParentId.Value].Children.Add(nodes[item.Id]);
            else
                roots.Add(nodes[item.Id]);
        }

        return roots;
    }
}
