using VaryoCms.Application.DTOs.ContentItem;
using VaryoCms.Application.Localization;
using VaryoCms.Application.Services;
using VaryoCms.Domain.Entities;
using VaryoCms.Domain.Enums;
using VaryoCms.Domain.Interfaces.Repositories;
using VaryoCms.Application.Interfaces;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Localization;
using NSubstitute;

namespace VaryoCms.Tests.Services;

public class ContentItemServiceTests
{
    private readonly IContentItemRepository _items = Substitute.For<IContentItemRepository>();
    private readonly IContentFieldValueRepository _values = Substitute.For<IContentFieldValueRepository>();
    private readonly IContentFieldRepository _fields = Substitute.For<IContentFieldRepository>();
    private readonly IContentRelationRepository _relations = Substitute.For<IContentRelationRepository>();
    private readonly IMediaRepository _media = Substitute.For<IMediaRepository>();
    private readonly IContentItemTitleRepository _titles = Substitute.For<IContentItemTitleRepository>();
    private readonly ICurrentUserContext _currentUser = Substitute.For<ICurrentUserContext>();
    private readonly IValidator<SaveContentItemRequest> _validator = Substitute.For<IValidator<SaveContentItemRequest>>();
    private readonly IStringLocalizer<SharedResource> _t = Substitute.For<IStringLocalizer<SharedResource>>();
    private readonly IAuditLogger _audit = Substitute.For<IAuditLogger>();

    public ContentItemServiceTests()
    {
        _validator.ValidateAsync(Arg.Any<SaveContentItemRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());   // valid by default

        // Return a string that includes the arguments (e.g. the field name) so message assertions hold.
        _t[Arg.Any<string>(), Arg.Any<object[]>()].Returns(ci =>
            new LocalizedString(ci.ArgAt<string>(0), string.Join(", ", ci.ArgAt<object[]>(1))));
        _t[Arg.Any<string>()].Returns(ci => new LocalizedString(ci.ArgAt<string>(0), ci.ArgAt<string>(0)));

        _currentUser.UserId.Returns((int?)42);
    }

    private ContentItemService Sut() => new(_items, _values, _fields, _relations, _media, _titles, _currentUser, _validator, _t, _audit);

    private void Fields(params ContentField[] fields) =>
        _fields.GetByContentTypeAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(fields.ToList());

    [Fact]
    public async Task Create_fails_when_required_field_missing()
    {
        Fields(new ContentField { Id = 1, Name = "Body", FieldType = FieldType.Text, IsRequired = true });
        var request = new SaveContentItemRequest { ContentTypeId = 1, Title = "My Item", Values = new() };

        var result = await Sut().CreateAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Contains("Body", result.Error);
        await _items.DidNotReceive().CreateAsync(Arg.Any<ContentItem>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_fails_when_required_relation_has_no_target()
    {
        Fields(new ContentField { Id = 2, Name = "Category", FieldType = FieldType.Relation, IsRequired = true });
        var request = new SaveContentItemRequest { ContentTypeId = 1, Title = "My Item", Relations = new() };

        var result = await Sut().CreateAsync(request);

        Assert.False(result.IsSuccess);
        Assert.Contains("Category", result.Error);
    }

    [Fact]
    public async Task Create_caps_single_relation_to_first_target()
    {
        Fields(new ContentField { Id = 2, Name = "Category", FieldType = FieldType.Relation, IsRequired = false });
        _items.CreateAsync(Arg.Any<ContentItem>(), Arg.Any<CancellationToken>()).Returns(100);
        var request = new SaveContentItemRequest
        {
            ContentTypeId = 1, Title = "My Item",
            Relations = new() { [2] = new List<int> { 10, 20, 30 } }
        };

        var result = await Sut().CreateAsync(request);

        Assert.True(result.IsSuccess);
        await _relations.Received().ReplaceAsync(100, 2,
            Arg.Is<IReadOnlyList<int>>(l => l.Count == 1 && l[0] == 10), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_keeps_all_targets_for_multirelation()
    {
        Fields(new ContentField { Id = 3, Name = "Tags", FieldType = FieldType.MultiRelation, IsRequired = false });
        _items.CreateAsync(Arg.Any<ContentItem>(), Arg.Any<CancellationToken>()).Returns(100);
        var request = new SaveContentItemRequest
        {
            ContentTypeId = 1, Title = "My Item",
            Relations = new() { [3] = new List<int> { 10, 20, 30 } }
        };

        await Sut().CreateAsync(request);

        await _relations.Received().ReplaceAsync(100, 3,
            Arg.Is<IReadOnlyList<int>>(l => l.Count == 3), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_persists_scalar_values_and_returns_id()
    {
        Fields(new ContentField { Id = 1, Name = "Body", FieldType = FieldType.Text, IsRequired = false, IsLocalized = true });
        _items.CreateAsync(Arg.Any<ContentItem>(), Arg.Any<CancellationToken>()).Returns(55);
        var request = new SaveContentItemRequest
        {
            ContentTypeId = 1, LanguageCode = "tr", Title = "My Item",
            Values = new() { [1] = "Hello" }
        };

        var result = await Sut().CreateAsync(request);

        Assert.True(result.IsSuccess);
        Assert.Equal(55, result.Value);
        await _values.Received().SaveValuesAsync(55,
            Arg.Is<List<ContentFieldValue>>(vs => vs.Count == 1 && vs[0].ValueText == "Hello"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_sets_created_by_and_updated_by_from_current_user()
    {
        Fields();
        _items.CreateAsync(Arg.Any<ContentItem>(), Arg.Any<CancellationToken>()).Returns(10);
        var request = new SaveContentItemRequest { ContentTypeId = 1, Title = "New Item" };

        await Sut().CreateAsync(request);

        await _items.Received().CreateAsync(
            Arg.Is<ContentItem>(ci => ci.CreatedBy == 42 && ci.UpdatedBy == 42),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_derives_slug_from_title_when_slug_not_provided()
    {
        Fields();
        _items.CreateAsync(Arg.Any<ContentItem>(), Arg.Any<CancellationToken>()).Returns(10);
        var request = new SaveContentItemRequest { ContentTypeId = 1, Title = "Merhaba Dünya" };

        await Sut().CreateAsync(request);

        await _items.Received().CreateAsync(
            Arg.Is<ContentItem>(ci => ci.Slug == "merhaba-dunya"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_saves_title_for_language()
    {
        Fields();
        _items.CreateAsync(Arg.Any<ContentItem>(), Arg.Any<CancellationToken>()).Returns(77);
        var request = new SaveContentItemRequest { ContentTypeId = 1, LanguageCode = "tr", Title = "Başlık" };

        await Sut().CreateAsync(request);

        await _titles.Received().SaveTitleAsync(77, "tr", "Başlık", Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }
}
