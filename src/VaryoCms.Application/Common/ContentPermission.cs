namespace VaryoCms.Application.Common;

// The CRUD action being authorized against a content type (maps to user_content_type_permissions columns).
public enum ContentPermission
{
    Read,
    Create,
    Update,
    Delete
}
