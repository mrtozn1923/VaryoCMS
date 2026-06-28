using VaryoCms.Application.Common;
using VaryoCms.Application.DTOs.ContentField;
using VaryoCms.Application.Interfaces;
using VaryoCms.Application.Localization;
using VaryoCms.Domain.Entities;
using VaryoCms.Domain.Interfaces.Repositories;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace VaryoCms.Application.Services;

public class ContentFieldService : IContentFieldService
{
    private readonly IContentFieldRepository _repository;
    private readonly IValidator<CreateContentFieldRequest> _createValidator;
    private readonly IValidator<UpdateContentFieldRequest> _updateValidator;
    private readonly IStringLocalizer<SharedResource> _t;
    private readonly IAuditLogger _audit;

    public ContentFieldService(
        IContentFieldRepository repository,
        IValidator<CreateContentFieldRequest> createValidator,
        IValidator<UpdateContentFieldRequest> updateValidator,
        IStringLocalizer<SharedResource> t,
        IAuditLogger audit)
    {
        _repository = repository;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _t = t;
        _audit = audit;
    }

    public async Task<Result<IReadOnlyList<ContentFieldDto>>> GetByContentTypeAsync(int contentTypeId, CancellationToken ct = default)
    {
        var entities = await _repository.GetByContentTypeAsync(contentTypeId, ct);
        IReadOnlyList<ContentFieldDto> dtos = entities.Select(MapToDto).ToList();
        return Result<IReadOnlyList<ContentFieldDto>>.Success(dtos);
    }

    public async Task<Result<ContentFieldDto>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var entity = await _repository.GetByIdAsync(id, ct);
        return entity is null
            ? Result<ContentFieldDto>.Failure(_t["Err.ContentFieldNotFound"])
            : Result<ContentFieldDto>.Success(MapToDto(entity));
    }

    public async Task<Result<int>> CreateAsync(CreateContentFieldRequest request, CancellationToken ct = default)
    {
        var validation = await _createValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Result<int>.Failure(FirstError(validation));

        if (await _repository.SlugExistsAsync(request.ContentTypeId, request.Slug, excludeId: null, ct))
            return Result<int>.Failure(_t["Err.FieldSlugInUse", request.Slug]);

        var entity = new ContentField
        {
            ContentTypeId = request.ContentTypeId,
            Name = request.Name,
            Slug = request.Slug,
            FieldType = request.FieldType,
            IsRequired = request.IsRequired,
            IsLocalized = request.IsLocalized,
            OptionsJson = request.OptionsJson
        };

        int newId = await _repository.CreateAsync(entity, ct);
        await _audit.LogAsync(AuditActions.ContentFieldCreated, "ContentField", newId,
            contentTypeId: entity.ContentTypeId, entityName: entity.Name, ct: ct);
        return Result<int>.Success(newId);
    }

    public async Task<Result> UpdateAsync(int id, UpdateContentFieldRequest request, CancellationToken ct = default)
    {
        var validation = await _updateValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Result.Failure(FirstError(validation));

        var existing = await _repository.GetByIdAsync(id, ct);
        if (existing is null)
            return Result.Failure(_t["Err.ContentFieldNotFound"]);

        if (await _repository.SlugExistsAsync(existing.ContentTypeId, request.Slug, excludeId: id, ct))
            return Result.Failure(_t["Err.FieldSlugInUse", request.Slug]);

        existing.Name = request.Name;
        existing.Slug = request.Slug;
        existing.FieldType = request.FieldType;
        existing.IsRequired = request.IsRequired;
        existing.IsLocalized = request.IsLocalized;
        existing.OptionsJson = request.OptionsJson;

        bool updated = await _repository.UpdateAsync(existing, ct);
        if (!updated) return Result.Failure(_t["Err.UpdateFailed"]);
        await _audit.LogAsync(AuditActions.ContentFieldUpdated, "ContentField", id,
            contentTypeId: existing.ContentTypeId, entityName: existing.Name, ct: ct);
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct = default)
    {
        var existing = await _repository.GetByIdAsync(id, ct);
        bool deleted = await _repository.SoftDeleteAsync(id, ct);
        if (!deleted) return Result.Failure(_t["Err.ContentFieldNotFound"]);
        await _audit.LogAsync(AuditActions.ContentFieldDeleted, "ContentField", id,
            contentTypeId: existing?.ContentTypeId, entityName: existing?.Name, ct: ct);
        return Result.Success();
    }

    public async Task<Result> ReorderAsync(int contentTypeId, ReorderFieldsRequest request, CancellationToken ct = default)
    {
        if (request.FieldIds is null || request.FieldIds.Count == 0)
            return Result.Failure(_t["Err.NoFieldOrder"]);

        await _repository.ReorderAsync(contentTypeId, request.FieldIds, ct);
        await _audit.LogAsync(AuditActions.ContentFieldReordered, "ContentField", contentTypeId,
            contentTypeId: contentTypeId, ct: ct);
        return Result.Success();
    }

    private static string FirstError(FluentValidation.Results.ValidationResult result)
        => result.Errors.FirstOrDefault()?.ErrorMessage ?? "Validation failed.";

    private static ContentFieldDto MapToDto(ContentField e) => new()
    {
        Id = e.Id,
        ContentTypeId = e.ContentTypeId,
        Name = e.Name,
        Slug = e.Slug,
        FieldType = e.FieldType,
        IsRequired = e.IsRequired,
        IsLocalized = e.IsLocalized,
        SortOrder = e.SortOrder,
        OptionsJson = e.OptionsJson
    };
}
