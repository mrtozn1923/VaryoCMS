namespace VaryoCms.Domain.Interfaces.Repositories;

public interface IApiDocRepository
{
    // Returns the BCrypt hash for an API key credential (without CT filter).
    Task<string?> GetApiKeyHashAsync(int tenantId, int credentialId, CancellationToken ct);

    // Returns enabled content type configs for the given slugs.
    Task<IReadOnlyList<ApiDocGroupRow>> GetGroupsAsync(int tenantId, IReadOnlyList<string> ctSlugs, CancellationToken ct);

    // Returns visible fields (after api_field_visibility) for the given content type slugs.
    Task<IReadOnlyList<ApiDocFieldRow>> GetFieldsAsync(int tenantId, IReadOnlyList<string> ctSlugs, CancellationToken ct);
}

public sealed record ApiDocGroupRow(
    string ContentTypeName,
    string ContentTypeSlug,
    bool AllowRead,
    bool AllowCreate,
    bool AllowUpdate,
    bool AllowDelete
);

public sealed record ApiDocFieldRow(
    string ContentTypeSlug,
    string FieldName,
    string FieldSlug,
    string FieldType,
    bool IsRequired,
    bool IsLocalized,
    string? Alias
);
