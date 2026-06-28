namespace VaryoCms.Application.Common;

public static class AuditActions
{
    public const string LoginSuccess          = "Auth.LoginSuccess";
    public const string LoginFailed           = "Auth.LoginFailed";
    public const string Logout                = "Auth.Logout";
    public const string PasswordChanged       = "Auth.PasswordChanged";
    public const string LoginCodeSent         = "Auth.CodeSent";
    public const string LoginCodeVerified     = "Auth.CodeVerified";
    public const string LoginCodeFailed       = "Auth.CodeFailed";

    public const string ContentItemCreated = "ContentItem.Created";
    public const string ContentItemUpdated = "ContentItem.Updated";
    public const string ContentItemDeleted = "ContentItem.Deleted";

    public const string ContentTypeCreated  = "ContentType.Created";
    public const string ContentTypeUpdated  = "ContentType.Updated";
    public const string ContentTypeDeleted  = "ContentType.Deleted";
    public const string ContentTypePublishToggled = "ContentType.PublishToggled";

    public const string ContentFieldCreated  = "ContentField.Created";
    public const string ContentFieldUpdated  = "ContentField.Updated";
    public const string ContentFieldDeleted  = "ContentField.Deleted";
    public const string ContentFieldReordered = "ContentField.Reordered";

    public const string MediaUploaded = "Media.Uploaded";
    public const string MediaRenamed  = "Media.Renamed";
    public const string MediaDeleted  = "Media.Deleted";
    public const string MediaCropped  = "Media.Cropped";

    public const string UserCreated  = "User.Created";
    public const string UserUpdated  = "User.Updated";
    public const string UserDeleted  = "User.Deleted";
    public const string UserPermissionsUpdated = "User.PermissionsUpdated";

    public const string DictionaryCreated = "Dictionary.Created";
    public const string DictionaryUpdated = "Dictionary.Updated";
    public const string DictionaryDeleted = "Dictionary.Deleted";

    public const string ApiCredentialCreated = "ApiCredential.Created";
    public const string ApiCredentialUpdated = "ApiCredential.Updated";
    public const string ApiCredentialRotated = "ApiCredential.Rotated";
    public const string ApiCredentialDeleted = "ApiCredential.Deleted";

    public const string ApiAuthFailed              = "Api.AuthFailed";
    public const string ApiVerbForbidden           = "Api.VerbForbidden";

    public const string SystemTenantCreated        = "System.TenantCreated";
    public const string SystemTenantUpdated        = "System.TenantUpdated";
    public const string SystemTenantDeleted        = "System.TenantDeleted";
    public const string SystemImpersonationStarted = "System.ImpersonationStarted";
    public const string SystemImpersonationExited  = "System.ImpersonationExited";
}
