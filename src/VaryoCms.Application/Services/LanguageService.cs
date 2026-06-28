using VaryoCms.Application.Common;
using VaryoCms.Application.DTOs.Localization;
using VaryoCms.Application.Interfaces;
using VaryoCms.Application.Localization;
using VaryoCms.Domain.Entities;
using VaryoCms.Domain.Interfaces.Repositories;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace VaryoCms.Application.Services;

public class LanguageService : ILanguageService
{
    private readonly ILanguageRepository _languages;
    private readonly IValidator<CreateLanguageRequest> _createValidator;
    private readonly IValidator<UpdateLanguageRequest> _updateValidator;
    private readonly IStringLocalizer<SharedResource> _t;

    public LanguageService(
        ILanguageRepository languages,
        IValidator<CreateLanguageRequest> createValidator,
        IValidator<UpdateLanguageRequest> updateValidator,
        IStringLocalizer<SharedResource> t)
    {
        _languages = languages;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _t = t;
    }

    public async Task<IReadOnlyList<LanguageDto>> GetActiveAsync(CancellationToken ct = default)
    {
        var langs = await _languages.GetActiveAsync(ct);
        return langs.Select(l => new LanguageDto
        {
            Code = l.Code.Trim(),   // CHAR(5) is space-padded — trim before use (see [[char5-language-code-trim]]).
            Name = l.Name,
            IsDefault = l.IsDefault
        }).ToList();
    }

    public async Task<IReadOnlyList<LanguageListItemDto>> GetListAsync(CancellationToken ct = default)
    {
        var langs = await _languages.GetAllAsync(ct);
        return langs.Select(l => new LanguageListItemDto
        {
            Id = l.Id,
            Code = l.Code.Trim(),
            Name = l.Name,
            IsDefault = l.IsDefault,
            IsActive = l.IsActive,
            FlagIcon = l.FlagIcon
        }).ToList();
    }

    public async Task<Result<LanguageEditDto>> GetForEditAsync(int id, CancellationToken ct = default)
    {
        var l = await _languages.GetByIdAsync(id, ct);
        return l is null
            ? Result<LanguageEditDto>.Failure(_t["Err.LanguageNotFound"])
            : Result<LanguageEditDto>.Success(new LanguageEditDto
            {
                Id = l.Id,
                Code = l.Code.Trim(),
                Name = l.Name,
                IsDefault = l.IsDefault,
                IsActive = l.IsActive,
                FlagIcon = l.FlagIcon
            });
    }

    public async Task<Result<int>> CreateAsync(CreateLanguageRequest request, CancellationToken ct = default)
    {
        var validation = await _createValidator.ValidateAsync(request, ct);
        if (!validation.IsValid) return Result<int>.Failure(FirstError(validation));

        var code = request.Code.Trim().ToLowerInvariant();
        if (await _languages.CodeExistsAsync(code, excludeId: null, ct))
            return Result<int>.Failure(_t["Err.LanguageCodeExists", code]);

        int id = await _languages.CreateAsync(new Language
        {
            Code = code,
            Name = request.Name.Trim(),
            FlagIcon = string.IsNullOrWhiteSpace(request.FlagIcon) ? null : request.FlagIcon.Trim(),
            IsDefault = request.IsDefault,
            IsActive = request.IsActive
        }, ct);
        return Result<int>.Success(id);
    }

    public async Task<Result> UpdateAsync(int id, UpdateLanguageRequest request, CancellationToken ct = default)
    {
        var validation = await _updateValidator.ValidateAsync(request, ct);
        if (!validation.IsValid) return Result.Failure(FirstError(validation));

        var existing = await _languages.GetByIdAsync(id, ct);
        if (existing is null) return Result.Failure(_t["Err.LanguageNotFound"]);

        // A default language can't be deactivated; deactivating must leave at least one active language.
        if (!request.IsActive && !request.IsDefault)
        {
            if (existing.IsDefault) return Result.Failure(_t["Err.LanguageDefaultDeactivate"]);
            if (existing.IsActive && await _languages.ActiveCountAsync(ct) <= 1)
                return Result.Failure(_t["Err.LanguageLastActive"]);
        }

        bool ok = await _languages.UpdateAsync(new Language
        {
            Id = id,
            Name = request.Name.Trim(),
            FlagIcon = string.IsNullOrWhiteSpace(request.FlagIcon) ? null : request.FlagIcon.Trim(),
            IsDefault = request.IsDefault,
            IsActive = request.IsActive
        }, ct);
        return ok ? Result.Success() : Result.Failure(_t["Err.LanguageNotFound"]);
    }

    public async Task<Result> SetActiveAsync(int id, bool isActive, CancellationToken ct = default)
    {
        var existing = await _languages.GetByIdAsync(id, ct);
        if (existing is null) return Result.Failure(_t["Err.LanguageNotFound"]);

        if (!isActive)
        {
            if (existing.IsDefault) return Result.Failure(_t["Err.LanguageDefaultDeactivate"]);
            if (await _languages.ActiveCountAsync(ct) <= 1)
                return Result.Failure(_t["Err.LanguageLastActive"]);
        }

        bool ok = await _languages.SetActiveAsync(id, isActive, ct);
        return ok ? Result.Success() : Result.Failure(_t["Err.LanguageNotFound"]);
    }

    private static string FirstError(FluentValidation.Results.ValidationResult result)
        => result.Errors.FirstOrDefault()?.ErrorMessage ?? "Validation failed.";
}
