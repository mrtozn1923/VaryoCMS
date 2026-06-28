using System.Security.Cryptography;
using VaryoCms.Application.Common;
using VaryoCms.Application.DTOs.Api;
using VaryoCms.Application.Interfaces;
using VaryoCms.Application.Localization;
using VaryoCms.Domain.Entities;
using VaryoCms.Domain.Enums;
using VaryoCms.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Localization;

namespace VaryoCms.Application.Services;

public class ApiCredentialService : IApiCredentialService
{
    private const int JwtLifetimeDays = 365;

    private readonly IApiCredentialRepository _credRepo;
    private readonly IContentTypeRepository _contentTypes;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwt;
    private readonly ITenantContext _tenant;
    private readonly IStringLocalizer<SharedResource> _t;
    private readonly IAuditLogger _audit;

    public ApiCredentialService(
        IApiCredentialRepository credRepo,
        IContentTypeRepository contentTypes,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwt,
        ITenantContext tenant,
        IStringLocalizer<SharedResource> t,
        IAuditLogger audit)
    {
        _credRepo = credRepo;
        _contentTypes = contentTypes;
        _passwordHasher = passwordHasher;
        _jwt = jwt;
        _tenant = tenant;
        _t = t;
        _audit = audit;
    }

    public async Task<Result<IReadOnlyList<ApiCredentialListItemDto>>> GetListAsync(CancellationToken ct = default)
    {
        var rows = await _credRepo.GetAllAsync(ct);
        IReadOnlyList<ApiCredentialListItemDto> list = rows.Select(r => new ApiCredentialListItemDto
        {
            Id = r.Credential.Id,
            Name = r.Credential.Name,
            AuthType = r.Credential.AuthType.ToString(),
            IsActive = r.Credential.IsActive,
            GrantedCount = r.GrantedCount,
            CreatedAt = r.Credential.CreatedAt
        }).ToList();
        return Result<IReadOnlyList<ApiCredentialListItemDto>>.Success(list);
    }

    public async Task<Result<ApiCredentialEditDto>> GetForEditAsync(int? id, CancellationToken ct = default)
    {
        var allContentTypes = await _contentTypes.GetAllAsync(ct);

        if (id is null or 0)
        {
            // Create form — all content types unchecked
            return Result<ApiCredentialEditDto>.Success(new ApiCredentialEditDto
            {
                ContentTypes = allContentTypes.Select(c => new CredentialContentTypeDto
                {
                    ContentTypeId = c.Id,
                    Name = c.Name,
                    Slug = c.Slug,
                    IsGranted = false
                }).ToList()
            });
        }

        var found = await _credRepo.GetByIdAsync(id.Value, ct);
        if (found is null) return Result<ApiCredentialEditDto>.Failure(_t["Err.CredentialNotFound"]);
        var (cred, grantedIds) = found.Value;
        var grantedSet = new HashSet<int>(grantedIds);

        return Result<ApiCredentialEditDto>.Success(new ApiCredentialEditDto
        {
            Id = cred.Id,
            Name = cred.Name,
            AuthType = cred.AuthType.ToString(),
            IsActive = cred.IsActive,
            HasApiKey = !string.IsNullOrEmpty(cred.ApiKey),
            ContentTypes = allContentTypes.Select(c => new CredentialContentTypeDto
            {
                ContentTypeId = c.Id,
                Name = c.Name,
                Slug = c.Slug,
                IsGranted = grantedSet.Contains(c.Id)
            }).ToList()
        });
    }

    public async Task<Result<ApiCredentialSaveResult>> SaveAsync(SaveApiCredentialRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Result<ApiCredentialSaveResult>.Failure(_t["Err.CredentialNameRequired"]);
        if (!Enum.TryParse<ApiAuthType>(request.AuthType, ignoreCase: true, out var authType) || authType == ApiAuthType.None)
            return Result<ApiCredentialSaveResult>.Failure(_t["Err.CredentialAuthTypeRequired"]);
        if (request.ContentTypeIds.Count == 0)
            return Result<ApiCredentialSaveResult>.Failure(_t["Err.CredentialNoContentTypes"]);

        string plaintextKey = string.Empty;
        int credId;

        if (request.Id == 0)
        {
            // Create
            var cred = new ApiCredential
            {
                Name = request.Name.Trim(),
                AuthType = authType,
                IsActive = request.IsActive
            };
            credId = await _credRepo.CreateAsync(cred, ct);
            await _credRepo.ReplaceContentTypesAsync(credId, request.ContentTypeIds, ct);

            if (authType == ApiAuthType.ApiKey)
            {
                // Generate key with id-prefix format: vk_{id}_{secret}
                string secret = GenerateSecret();
                string fullKey = $"vk_{credId}_{secret}";
                await _credRepo.UpdateApiKeyAsync(credId, _passwordHasher.Hash(secret), ct);
                plaintextKey = fullKey;
            }
        }
        else
        {
            credId = request.Id;
            // Update (auth_type is immutable)
            var found = await _credRepo.GetByIdAsync(credId, ct);
            if (found is null) return Result<ApiCredentialSaveResult>.Failure(_t["Err.CredentialNotFound"]);

            var cred = found.Value.Credential;
            cred.Name = request.Name.Trim();
            cred.IsActive = request.IsActive;
            await _credRepo.UpdateAsync(cred, ct);
            await _credRepo.ReplaceContentTypesAsync(credId, request.ContentTypeIds, ct);
        }

        string auditAction = request.Id == 0 ? AuditActions.ApiCredentialCreated : AuditActions.ApiCredentialUpdated;
        await _audit.LogAsync(auditAction, "ApiCredential", credId, entityName: request.Name.Trim(), ct: ct);
        return Result<ApiCredentialSaveResult>.Success(new ApiCredentialSaveResult(credId, plaintextKey));
    }

    public async Task<Result<string>> RotateApiKeyAsync(int credentialId, CancellationToken ct = default)
    {
        var found = await _credRepo.GetByIdAsync(credentialId, ct);
        if (found is null) return Result<string>.Failure(_t["Err.CredentialNotFound"]);
        if (found.Value.Credential.AuthType != ApiAuthType.ApiKey)
            return Result<string>.Failure(_t["Err.CredentialApiKeyOnly"]);

        string secret = GenerateSecret();
        string fullKey = $"vk_{credentialId}_{secret}";
        await _credRepo.UpdateApiKeyAsync(credentialId, _passwordHasher.Hash(secret), ct);
        await _audit.LogAsync(AuditActions.ApiCredentialRotated, "ApiCredential", credentialId,
            entityName: found.Value.Credential.Name, ct: ct);
        return Result<string>.Success(fullKey);
    }

    public async Task<Result<string>> IssueJwtAsync(int credentialId, CancellationToken ct = default)
    {
        var found = await _credRepo.GetByIdAsync(credentialId, ct);
        if (found is null) return Result<string>.Failure(_t["Err.CredentialNotFound"]);
        if (found.Value.Credential.AuthType != ApiAuthType.JWT)
            return Result<string>.Failure(_t["Err.CredentialJwtOnly"]);

        // Load the slugs for all granted content types — the JWT covers all of them.
        var contentTypes = await _contentTypes.GetAllAsync(ct);
        var grantedIds = new HashSet<int>(found.Value.ContentTypeIds);
        var slugs = contentTypes.Where(c => grantedIds.Contains(c.Id)).Select(c => c.Slug).ToList();

        string token = _jwt.IssueToken(_tenant.TenantSlug, slugs, TimeSpan.FromDays(JwtLifetimeDays));
        return Result<string>.Success(token);
    }

    public async Task<Result> DeleteAsync(int credentialId, CancellationToken ct = default)
    {
        var found = await _credRepo.GetByIdAsync(credentialId, ct);
        if (found is null) return Result.Failure(_t["Err.CredentialNotFound"]);
        await _credRepo.DeleteAsync(credentialId, ct);
        await _audit.LogAsync(AuditActions.ApiCredentialDeleted, "ApiCredential", credentialId,
            entityName: found.Value.Credential.Name, ct: ct);
        return Result.Success();
    }

    // Generates the secret portion of the API key (32 random bytes, URL-safe base64).
    private static string GenerateSecret()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
