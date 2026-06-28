using VaryoCms.Application.Common;
using VaryoCms.Application.DTOs.ContentType;
using VaryoCms.Application.Interfaces;
using VaryoCms.Application.Localization;
using VaryoCms.Domain.Entities;
using VaryoCms.Domain.Interfaces.Repositories;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace VaryoCms.Application.Services;

public class ContentTypeService : IContentTypeService
{
    private readonly IContentTypeRepository _repository;
    private readonly IValidator<CreateContentTypeRequest> _createValidator;
    private readonly IValidator<UpdateContentTypeRequest> _updateValidator;
    private readonly IStringLocalizer<SharedResource> _t;
    private readonly IAuditLogger _audit;

    public ContentTypeService(
        IContentTypeRepository repository,
        IValidator<CreateContentTypeRequest> createValidator,
        IValidator<UpdateContentTypeRequest> updateValidator,
        IStringLocalizer<SharedResource> t,
        IAuditLogger audit)
    {
        _repository = repository;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _t = t;
        _audit = audit;
    }

    public async Task<Result<IReadOnlyList<ContentTypeDto>>> GetAllAsync(CancellationToken ct = default)
    {
        var entities = await _repository.GetAllAsync(ct);
        IReadOnlyList<ContentTypeDto> dtos = entities.Select(MapToDto).ToList();
        return Result<IReadOnlyList<ContentTypeDto>>.Success(dtos);
    }

    public async Task<Result<ContentTypeDto>> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var entity = await _repository.GetByIdAsync(id, ct);
        return entity is null
            ? Result<ContentTypeDto>.Failure(_t["Err.ContentTypeNotFound"])
            : Result<ContentTypeDto>.Success(MapToDto(entity));
    }

    public async Task<Result<int>> CreateAsync(CreateContentTypeRequest request, CancellationToken ct = default)
    {
        var validation = await _createValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Result<int>.Failure(FirstError(validation));

        if (await _repository.SlugExistsAsync(request.Slug, excludeId: null, ct))
            return Result<int>.Failure(_t["Err.SlugInUse", request.Slug]);

        if (request.ParentId.HasValue)
        {
            var parent = await _repository.GetByIdAsync(request.ParentId.Value, ct);
            if (parent is null) return Result<int>.Failure(_t["Err.ContentTypeNotFound"]);
        }

        var entity = new ContentType
        {
            Name = request.Name,
            Slug = request.Slug,
            Description = request.Description,
            Icon = request.Icon,
            IsPublished = request.IsPublished,
            SortOrder = request.SortOrder,
            ParentId = request.ParentId
        };

        int newId = await _repository.CreateAsync(entity, ct);
        await _audit.LogAsync(AuditActions.ContentTypeCreated, "ContentType", newId, entityName: entity.Name, ct: ct);
        return Result<int>.Success(newId);
    }

    public async Task<Result> UpdateAsync(int id, UpdateContentTypeRequest request, CancellationToken ct = default)
    {
        var validation = await _updateValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return Result.Failure(FirstError(validation));

        var existing = await _repository.GetByIdAsync(id, ct);
        if (existing is null)
            return Result.Failure(_t["Err.ContentTypeNotFound"]);

        if (await _repository.SlugExistsAsync(request.Slug, excludeId: id, ct))
            return Result.Failure(_t["Err.SlugInUse", request.Slug]);

        if (request.ParentId.HasValue)
        {
            if (request.ParentId.Value == id)
                return Result.Failure(_t["Err.ContentTypeSelfParent"]);
            var parent = await _repository.GetByIdAsync(request.ParentId.Value, ct);
            if (parent is null) return Result.Failure(_t["Err.ContentTypeNotFound"]);
            // Prevent direct cycle: parent cannot point back to this CT
            if (parent.ParentId == id)
                return Result.Failure(_t["Err.ContentTypeCyclicParent"]);
        }

        existing.Name = request.Name;
        existing.Slug = request.Slug;
        existing.Description = request.Description;
        existing.Icon = request.Icon;
        existing.IsPublished = request.IsPublished;
        existing.SortOrder = request.SortOrder;
        existing.ParentId = request.ParentId;

        bool updated = await _repository.UpdateAsync(existing, ct);
        if (!updated) return Result.Failure(_t["Err.UpdateFailed"]);
        await _audit.LogAsync(AuditActions.ContentTypeUpdated, "ContentType", id, entityName: existing.Name, ct: ct);
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken ct = default)
    {
        var existing = await _repository.GetByIdAsync(id, ct);
        bool deleted = await _repository.SoftDeleteAsync(id, ct);
        if (!deleted) return Result.Failure(_t["Err.ContentTypeNotFound"]);
        await _audit.LogAsync(AuditActions.ContentTypeDeleted, "ContentType", id, entityName: existing?.Name, ct: ct);
        return Result.Success();
    }

    private static string FirstError(FluentValidation.Results.ValidationResult result)
        => result.Errors.FirstOrDefault()?.ErrorMessage ?? "Validation failed.";

    private static ContentTypeDto MapToDto(ContentType e) => new()
    {
        Id = e.Id,
        Name = e.Name,
        Slug = e.Slug,
        Description = e.Description,
        Icon = e.Icon,
        IsPublished = e.IsPublished,
        SortOrder = e.SortOrder,
        ParentId = e.ParentId,
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt
    };
}
