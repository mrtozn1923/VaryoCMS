using VaryoCms.Application.Common;
using VaryoCms.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace VaryoCms.Web.Authorization;

// Authorizes an action against the current user's per-content-type permission.
// Reads the {contentTypeId} route value; Forbid (→ AccessDenied) when the user lacks the permission.
[AttributeUsage(AttributeTargets.Method)]
public sealed class RequireContentTypePermissionAttribute : Attribute, IAsyncActionFilter
{
    private readonly ContentPermission _permission;

    public RequireContentTypePermissionAttribute(ContentPermission permission) => _permission = permission;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!int.TryParse(context.RouteData.Values["contentTypeId"]?.ToString(), out int contentTypeId))
        {
            context.Result = new BadRequestResult();
            return;
        }

        var permissions = context.HttpContext.RequestServices.GetRequiredService<IPermissionService>();
        if (!await permissions.HasPermissionAsync(contentTypeId, _permission))
        {
            context.Result = new ForbidResult();
            return;
        }

        await next();
    }
}
