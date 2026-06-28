namespace VaryoCms.Application.Common;

// The current user's CRUD flags for a single content type (used to show/hide UI actions).
public record ContentPermissionSet(bool CanRead, bool CanCreate, bool CanUpdate, bool CanDelete)
{
    public static readonly ContentPermissionSet None = new(false, false, false, false);
    public static readonly ContentPermissionSet All = new(true, true, true, true);
}
