using VaryoCms.Application.Common;
using VaryoCms.Application.DTOs.ApiDocs;
using VaryoCms.Application.Interfaces;
using VaryoCms.Domain.Interfaces.Repositories;
using static VaryoCms.Application.Common.Slugifier;

namespace VaryoCms.Application.Services;

public sealed class ApiDocService : IApiDocService
{
    private readonly IApiDocRepository _repo;
    private readonly IPublicApiRepository _publicApiRepo;
    private readonly ITenantStore _tenantStore;
    private readonly IJwtTokenService _jwt;
    private readonly IPasswordHasher _hasher;

    public ApiDocService(
        IApiDocRepository repo,
        IPublicApiRepository publicApiRepo,
        ITenantStore tenantStore,
        IJwtTokenService jwt,
        IPasswordHasher hasher)
    {
        _repo = repo;
        _publicApiRepo = publicApiRepo;
        _tenantStore = tenantStore;
        _jwt = jwt;
        _hasher = hasher;
    }

    public async Task<Result<ApiDocManifestDto>> GetManifestAsync(
        string tenantSlug, string credential, CancellationToken ct)
    {
        var tenant = await _tenantStore.FindBySlugAsync(tenantSlug);
        if (tenant is null)
            return Result<ApiDocManifestDto>.Failure("Tenant not found");

        IReadOnlyList<string> ctSlugs;

        if (credential.StartsWith("vk_", StringComparison.OrdinalIgnoreCase))
        {
            // API Key: vk_{credentialId}_{secret}
            var parts = credential.Split('_', 3);
            if (parts.Length != 3 || !int.TryParse(parts[1], out int credId) || string.IsNullOrEmpty(parts[2]))
                return Result<ApiDocManifestDto>.Failure("Invalid credential format");

            string? hash = await _repo.GetApiKeyHashAsync(tenant.Id, credId, ct);
            if (hash is null || !_hasher.Verify(parts[2], hash))
                return Result<ApiDocManifestDto>.Failure("Invalid or expired credential");

            ctSlugs = await _publicApiRepo.GetCredentialContentTypeSlugsAsync(tenant.Id, credId, ct);
        }
        else if (credential.StartsWith("eyJ", StringComparison.OrdinalIgnoreCase))
        {
            // JWT Bearer token
            var (isValid, jwtTenant, jwtCts) = _jwt.ExtractClaims(credential);
            if (!isValid || !string.Equals(jwtTenant, tenantSlug, StringComparison.OrdinalIgnoreCase))
                return Result<ApiDocManifestDto>.Failure("Invalid or expired token");

            ctSlugs = jwtCts;
        }
        else
        {
            return Result<ApiDocManifestDto>.Failure("Unrecognized credential. Provide an API key (vk_...) or JWT token (eyJ...).");
        }

        if (ctSlugs.Count == 0)
            return Result<ApiDocManifestDto>.Failure("No API endpoints are accessible with this credential");

        var ctSlugList = ctSlugs.ToList();
        var groups = await _repo.GetGroupsAsync(tenant.Id, ctSlugList, ct);
        var fields = await _repo.GetFieldsAsync(tenant.Id, ctSlugList, ct);

        var fieldsByCtSlug = fields
            .GroupBy(f => f.ContentTypeSlug, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        var manifest = new ApiDocManifestDto(
            tenantSlug,
            groups.Select(g => new ApiDocGroupDto(
                g.ContentTypeName,
                g.ContentTypeSlug,
                g.AllowRead,
                g.AllowCreate,
                g.AllowUpdate,
                g.AllowDelete,
                (fieldsByCtSlug.TryGetValue(g.ContentTypeSlug, out var fList) ? fList : [])
                    .Select(f => new ApiDocFieldDto(
                        f.FieldName,
                        f.FieldSlug,
                        ToCamelCase(f.Alias ?? f.FieldSlug),
                        f.FieldType,
                        FieldTypeToJsonType(f.FieldType),
                        f.IsRequired,
                        f.IsLocalized
                    )).ToList()
            )).ToList()
        );

        return Result<ApiDocManifestDto>.Success(manifest);
    }

    private static string FieldTypeToJsonType(string fieldType) => fieldType switch
    {
        "Text" or "Email" or "URL" or "Phone" or "Color" or "Slug" or "Password" => "string",
        "RichText" => "string (HTML)",
        "Markdown" => "string (Markdown)",
        "CodeSnippet" => "string (code)",
        "Number" or "Rating" => "number (integer)",
        "Decimal" => "number (decimal)",
        "Boolean" => "boolean",
        "Date" => "string (YYYY-MM-DD)",
        "DateTime" => "string (ISO 8601 datetime)",
        "Time" => "string (HH:mm)",
        "DateRange" => "{ start: string, end: string }",
        "JSON" => "any (JSON)",
        "Image" or "Video" or "Audio" or "File" => "{ id: number, url: string }",
        "Gallery" => "Array<{ id: number, url: string }>",
        "Select" => "string",
        "MultiSelect" => "string (comma-separated values)",
        "Tags" => "string[] (JSON array)",
        "Relation" => "{ id: number, displayValue: string }",
        "MultiRelation" => "Array<{ id: number, displayValue: string }>",
        "GeoLocation" => "{ lat: number, lng: number }",
        _ => "string"
    };
}
