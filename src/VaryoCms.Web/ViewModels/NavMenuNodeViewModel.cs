using VaryoCms.Application.DTOs.Navigation;

namespace VaryoCms.Web.ViewModels;

public class NavMenuNodeViewModel
{
    public AccessibleContentTypeDto ContentType { get; set; } = null!;
    public List<NavMenuNodeViewModel> Children { get; set; } = new();
    public bool HasChildren => Children.Count > 0;
}
