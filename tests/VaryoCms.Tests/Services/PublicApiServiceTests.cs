using System.Text.Json;
using VaryoCms.Application.Common;
using VaryoCms.Application.DTOs.Api;
using VaryoCms.Application.Interfaces;
using VaryoCms.Application.Services;
using VaryoCms.Domain.Entities;
using VaryoCms.Domain.Enums;
using VaryoCms.Domain.Interfaces.Repositories;
using NSubstitute;

namespace VaryoCms.Tests.Services;

public class PublicApiServiceTests
{
    private readonly ITenantStore _tenants = Substitute.For<ITenantStore>();
    private readonly IPublicApiRepository _repo = Substitute.For<IPublicApiRepository>();
    private readonly IPublicApiWriteRepository _writeRepo = Substitute.For<IPublicApiWriteRepository>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly IJwtTokenService _jwt = Substitute.For<IJwtTokenService>();
    private readonly IResponseCache _cache = Substitute.For<IResponseCache>();
    private readonly IAuditLogger _audit = Substitute.For<IAuditLogger>();

    private PublicApiService Sut() => new(_tenants, _repo, _writeRepo, _hasher, _jwt, _cache, _audit);

    private static readonly ApiCredentials NoCreds = new(null, null);

    // Wires a happy-path pipeline (tenant + enabled config + empty result).
    // isPublic = true: no credential needed. isPublic = false: credential required.
    private ApiConfiguration HappyPath(bool isPublic = true,
        IReadOnlyList<(ContentField, string?)>? fields = null,
        IReadOnlyList<ContentItem>? items = null)
    {
        _tenants.FindBySlugAsync("acme", Arg.Any<CancellationToken>())
            .Returns(new TenantInfo(1, "acme", "Acme", "tr"));
        var config = new ApiConfiguration
        {
            Id = 99, ContentTypeId = 10, IsEnabled = true, IsPublic = isPublic,
            AllowFiltering = true, AllowSorting = true, AllowPagination = true, CacheSeconds = 0
        };
        _repo.GetEnabledConfigAsync(1, "blog", Arg.Any<CancellationToken>()).Returns(config);
        _repo.GetVisibleFieldsAsync(99, Arg.Any<CancellationToken>())
            .Returns(fields ?? new List<(ContentField, string?)>());
        _repo.GetItemsAsync(Arg.Any<ApiItemQuery>(), Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<ContentItem>)(items ?? new List<ContentItem>()), items?.Count ?? 0));
        _repo.GetValuesAsync(Arg.Any<int>(), Arg.Any<IReadOnlyList<int>>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new List<ContentFieldValue>());
        return config;
    }

    [Fact]
    public async Task Unknown_tenant_returns_NotFound()
    {
        _tenants.FindBySlugAsync("nope", Arg.Any<CancellationToken>()).Returns((TenantInfo?)null);
        var r = await Sut().GetListAsync("nope", "blog", new ApiListRequest(), NoCreds);
        Assert.False(r.IsSuccess);
        Assert.Equal("NotFound", r.Error);
    }

    [Fact]
    public async Task Disabled_or_missing_config_returns_NotFound()
    {
        _tenants.FindBySlugAsync("acme", Arg.Any<CancellationToken>()).Returns(new TenantInfo(1, "acme", "Acme", "tr"));
        _repo.GetEnabledConfigAsync(1, "blog", Arg.Any<CancellationToken>()).Returns((ApiConfiguration?)null);
        var r = await Sut().GetListAsync("acme", "blog", new ApiListRequest(), NoCreds);
        Assert.Equal("NotFound", r.Error);
    }

    [Fact]
    public async Task Public_content_type_succeeds_without_credentials()
    {
        HappyPath(isPublic: true);
        var r = await Sut().GetListAsync("acme", "blog", new ApiListRequest(), NoCreds);
        Assert.True(r.IsSuccess);
    }

    [Fact]
    public async Task Protected_content_type_without_credentials_returns_Unauthorized()
    {
        HappyPath(isPublic: false);
        var r = await Sut().GetListAsync("acme", "blog", new ApiListRequest(), NoCreds);
        Assert.Equal("Unauthorized", r.Error);
    }

    [Fact]
    public async Task ApiKey_auth_requires_matching_key()
    {
        HappyPath(isPublic: false);
        // Good key: vk_7_correctsecret (id=7, secret="correctsecret")
        _repo.GetApiKeyGrantHashAsync(1, 7, 10, Arg.Any<CancellationToken>())
            .Returns("stored-hash");
        _hasher.Verify("correctsecret", "stored-hash").Returns(true);

        var ok = await Sut().GetListAsync("acme", "blog", new ApiListRequest(),
            new ApiCredentials("vk_7_correctsecret", null));
        Assert.True(ok.IsSuccess);

        // Wrong secret portion
        _repo.GetApiKeyGrantHashAsync(1, 7, 10, Arg.Any<CancellationToken>())
            .Returns("stored-hash");
        _hasher.Verify("wrongsecret", "stored-hash").Returns(false);
        var bad = await Sut().GetListAsync("acme", "blog", new ApiListRequest(),
            new ApiCredentials("vk_7_wrongsecret", null));
        Assert.Equal("Unauthorized", bad.Error);

        // No key at all
        var none = await Sut().GetListAsync("acme", "blog", new ApiListRequest(), NoCreds);
        Assert.Equal("Unauthorized", none.Error);
    }

    [Fact]
    public async Task ApiKey_not_covering_this_content_type_returns_Unauthorized()
    {
        HappyPath(isPublic: false);
        // Grant lookup returns null — credential doesn't cover this CT
        _repo.GetApiKeyGrantHashAsync(1, 7, 10, Arg.Any<CancellationToken>())
            .Returns((string?)null);

        var r = await Sut().GetListAsync("acme", "blog", new ApiListRequest(),
            new ApiCredentials("vk_7_anysecret", null));
        Assert.Equal("Unauthorized", r.Error);
    }

    [Fact]
    public async Task Jwt_auth_uses_token_service()
    {
        HappyPath(isPublic: false);
        _jwt.ValidateToken("tok", "acme", "blog").Returns(true);

        var ok = await Sut().GetListAsync("acme", "blog", new ApiListRequest(),
            new ApiCredentials(null, "tok"));
        Assert.True(ok.IsSuccess);

        _jwt.ValidateToken("bad", "acme", "blog").Returns(false);
        var bad = await Sut().GetListAsync("acme", "blog", new ApiListRequest(),
            new ApiCredentials(null, "bad"));
        Assert.Equal("Unauthorized", bad.Error);
    }

    [Fact]
    public async Task Sorting_disabled_falls_back_to_default_column()
    {
        var config = HappyPath(isPublic: true);
        config.AllowSorting = false;
        ApiItemQuery? captured = null;
        _repo.GetItemsAsync(Arg.Do<ApiItemQuery>(q => captured = q), Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<ContentItem>)new List<ContentItem>(), 0));

        await Sut().GetListAsync("acme", "blog", new ApiListRequest { Sort = "title:desc" }, NoCreds);

        Assert.Equal("created_at", captured!.SortItemColumn);
        Assert.Null(captured.SortFieldId);
    }

    [Fact]
    public async Task Item_column_sort_is_passed_through()
    {
        HappyPath(isPublic: true);
        ApiItemQuery? captured = null;
        _repo.GetItemsAsync(Arg.Do<ApiItemQuery>(q => captured = q), Arg.Any<CancellationToken>())
            .Returns(((IReadOnlyList<ContentItem>)new List<ContentItem>(), 0));

        await Sut().GetListAsync("acme", "blog", new ApiListRequest { Sort = "id:asc" }, NoCreds);

        Assert.Equal("id", captured!.SortItemColumn);
        Assert.False(captured.SortDesc);
    }

    [Fact]
    public async Task Relation_field_expands_to_id_and_display_value()
    {
        var relationField = new ContentField
        {
            Id = 20, Slug = "category", FieldType = FieldType.Relation,
            OptionsJson = "{\"target_content_type_id\":3,\"display_field_slug\":\"title\"}"
        };
        var item = new ContentItem { Id = 1, Slug = "post", Status = "published" };
        HappyPath(isPublic: true,
            fields: new List<(ContentField, string?)> { (relationField, null) },
            items: new List<ContentItem> { item });

        _repo.GetRelationsAsync(1, Arg.Any<IReadOnlyList<int>>(), Arg.Any<CancellationToken>())
            .Returns(new List<(int, int, int)> { (1, 20, 5) });
        _repo.GetDisplayValuesAsync(1, 3, "title", Arg.Any<IReadOnlyList<int>>(), "tr", Arg.Any<CancellationToken>())
            .Returns(new Dictionary<int, string> { [5] = "Technology" });

        var r = await Sut().GetListAsync("acme", "blog", new ApiListRequest(), NoCreds);

        Assert.True(r.IsSuccess);
        var json = JsonSerializer.Serialize(r.Value!.Data[0].Fields["category"]);
        Assert.Contains("\"id\":5", json);
        Assert.Contains("Technology", json);
    }
}
