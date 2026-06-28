using VaryoCms.Application.Common;
using VaryoCms.Application.DTOs.ContentType;
using VaryoCms.Application.Interfaces;
using VaryoCms.Application.Localization;
using VaryoCms.Application.Services;
using VaryoCms.Domain.Entities;
using VaryoCms.Domain.Interfaces.Repositories;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Localization;
using NSubstitute;

namespace VaryoCms.Tests.Services;

public class ContentTypeServiceTests
{
    private readonly IContentTypeRepository _repo = Substitute.For<IContentTypeRepository>();
    private readonly IValidator<CreateContentTypeRequest> _createVal = Substitute.For<IValidator<CreateContentTypeRequest>>();
    private readonly IValidator<UpdateContentTypeRequest> _updateVal = Substitute.For<IValidator<UpdateContentTypeRequest>>();
    private readonly IStringLocalizer<SharedResource> _t = Substitute.For<IStringLocalizer<SharedResource>>();
    private readonly IAuditLogger _audit = Substitute.For<IAuditLogger>();

    public ContentTypeServiceTests()
    {
        _createVal.ValidateAsync(Arg.Any<CreateContentTypeRequest>(), Arg.Any<CancellationToken>())
                  .Returns(new ValidationResult());
        _updateVal.ValidateAsync(Arg.Any<UpdateContentTypeRequest>(), Arg.Any<CancellationToken>())
                  .Returns(new ValidationResult());
        _t[Arg.Any<string>(), Arg.Any<object[]>()].Returns(ci => new LocalizedString(ci.ArgAt<string>(0), ci.ArgAt<string>(0)));
        _t[Arg.Any<string>()].Returns(ci => new LocalizedString(ci.ArgAt<string>(0), ci.ArgAt<string>(0)));
    }

    private ContentTypeService Sut() => new(_repo, _createVal, _updateVal, _t, _audit);

    private ContentType ExistingCt(int id = 1, string slug = "blog") => new()
    {
        Id = id, Name = "Blog", Slug = slug, IsPublished = true
    };

    [Fact]
    public async Task Create_fails_when_slug_already_exists()
    {
        _repo.SlugExistsAsync("blog", null, Arg.Any<CancellationToken>()).Returns(true);

        var result = await Sut().CreateAsync(new CreateContentTypeRequest
        {
            Name = "Blog", Slug = "blog"
        });

        Assert.False(result.IsSuccess);
        await _repo.DidNotReceive().CreateAsync(Arg.Any<ContentType>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_fails_when_validation_fails()
    {
        _createVal.ValidateAsync(Arg.Any<CreateContentTypeRequest>(), Arg.Any<CancellationToken>())
                  .Returns(new ValidationResult(new[] { new ValidationFailure("Slug", "Slug is required") }));

        var result = await Sut().CreateAsync(new CreateContentTypeRequest { Name = "X", Slug = "" });

        Assert.False(result.IsSuccess);
        Assert.Contains("Slug", result.Error);
    }

    [Fact]
    public async Task Create_succeeds_and_logs_audit()
    {
        _repo.SlugExistsAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<CancellationToken>()).Returns(false);
        _repo.CreateAsync(Arg.Any<ContentType>(), Arg.Any<CancellationToken>()).Returns(42);

        var result = await Sut().CreateAsync(new CreateContentTypeRequest
        {
            Name = "Blog", Slug = "blog", IsPublished = true
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
        await _audit.Received(1).LogAsync(AuditActions.ContentTypeCreated,
            "ContentType", 42, Arg.Any<int?>(), "Blog",
            Arg.Any<object?>(), Arg.Any<string?>(), Arg.Any<int?>(), Arg.Any<int?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_fails_when_parent_does_not_exist()
    {
        _repo.SlugExistsAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<CancellationToken>()).Returns(false);
        _repo.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((ContentType?)null);

        var result = await Sut().CreateAsync(new CreateContentTypeRequest
        {
            Name = "Child", Slug = "child", ParentId = 99
        });

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Update_fails_when_content_type_not_found()
    {
        _repo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((ContentType?)null);

        var result = await Sut().UpdateAsync(1, new UpdateContentTypeRequest { Name = "X", Slug = "x" });

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Update_fails_when_slug_taken_by_another_ct()
    {
        _repo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(ExistingCt());
        _repo.SlugExistsAsync("taken-slug", 1, Arg.Any<CancellationToken>()).Returns(true);

        var result = await Sut().UpdateAsync(1, new UpdateContentTypeRequest
        {
            Name = "Blog", Slug = "taken-slug"
        });

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Update_fails_when_self_parent()
    {
        _repo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(ExistingCt());
        _repo.SlugExistsAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<CancellationToken>()).Returns(false);

        var result = await Sut().UpdateAsync(1, new UpdateContentTypeRequest
        {
            Name = "Blog", Slug = "blog", ParentId = 1  // self as parent
        });

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Update_succeeds_and_logs_audit()
    {
        _repo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(ExistingCt());
        _repo.SlugExistsAsync(Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<CancellationToken>()).Returns(false);
        _repo.UpdateAsync(Arg.Any<ContentType>(), Arg.Any<CancellationToken>()).Returns(true);

        var result = await Sut().UpdateAsync(1, new UpdateContentTypeRequest
        {
            Name = "Blog Updated", Slug = "blog"
        });

        Assert.True(result.IsSuccess);
        await _audit.Received(1).LogAsync(AuditActions.ContentTypeUpdated,
            "ContentType", 1, Arg.Any<int?>(), Arg.Any<string?>(),
            Arg.Any<object?>(), Arg.Any<string?>(), Arg.Any<int?>(), Arg.Any<int?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetById_returns_failure_when_not_found()
    {
        _repo.GetByIdAsync(999, Arg.Any<CancellationToken>()).Returns((ContentType?)null);

        var result = await Sut().GetByIdAsync(999);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task GetById_maps_entity_to_dto()
    {
        _repo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(ExistingCt(1, "blog"));

        var result = await Sut().GetByIdAsync(1);

        Assert.True(result.IsSuccess);
        Assert.Equal("blog", result.Value!.Slug);
        Assert.Equal("Blog", result.Value.Name);
    }

    [Fact]
    public async Task Delete_returns_failure_when_not_found()
    {
        _repo.SoftDeleteAsync(99, Arg.Any<CancellationToken>()).Returns(false);

        var result = await Sut().DeleteAsync(99);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Delete_succeeds_and_logs_audit()
    {
        _repo.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(ExistingCt());
        _repo.SoftDeleteAsync(1, Arg.Any<CancellationToken>()).Returns(true);

        var result = await Sut().DeleteAsync(1);

        Assert.True(result.IsSuccess);
        await _audit.Received(1).LogAsync(AuditActions.ContentTypeDeleted,
            "ContentType", 1, Arg.Any<int?>(), Arg.Any<string?>(),
            Arg.Any<object?>(), Arg.Any<string?>(), Arg.Any<int?>(), Arg.Any<int?>(),
            Arg.Any<CancellationToken>());
    }
}
