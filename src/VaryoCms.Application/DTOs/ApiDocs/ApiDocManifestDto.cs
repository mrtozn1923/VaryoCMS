namespace VaryoCms.Application.DTOs.ApiDocs;

public sealed record ApiDocManifestDto(
    string TenantSlug,
    List<ApiDocGroupDto> Groups
);

public sealed record ApiDocGroupDto(
    string ContentTypeName,
    string ContentTypeSlug,
    bool AllowRead,
    bool AllowCreate,
    bool AllowUpdate,
    bool AllowDelete,
    List<ApiDocFieldDto> Fields
);

public sealed record ApiDocFieldDto(
    string Name,
    string Slug,
    string ApiKey,      // key used in JSON response (alias if set, otherwise slug)
    string FieldType,
    string JsonType,    // human-readable description for the docs
    bool IsRequired,
    bool IsLocalized
);
