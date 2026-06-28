using VaryoCms.Application.Common;
using VaryoCms.Application.Interfaces;
using VaryoCms.Application.Services;
using VaryoCms.Domain.Entities;
using VaryoCms.Domain.Enums;
using VaryoCms.Domain.Interfaces.Repositories;
using NSubstitute;

namespace VaryoCms.Tests.Services;

public class PermissionServiceTests
{
    private readonly ICurrentUserContext _user = Substitute.For<ICurrentUserContext>();
    private readonly IContentTypeRepository _contentTypes = Substitute.For<IContentTypeRepository>();
    private readonly IUserPermissionRepository _permissions = Substitute.For<IUserPermissionRepository>();

    private PermissionService Sut() => new(_user, _contentTypes, _permissions);

    private static ContentType Ct(int id, string name, bool published) =>
        new() { Id = id, Name = name, Slug = name.ToLowerInvariant(), IsPublished = published };

    [Fact]
    public async Task Admin_sees_all_published_content_types()
    {
        _user.IsAuthenticated.Returns(true);
        _user.IsAdmin.Returns(true);
        _contentTypes.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<ContentType> { Ct(1, "Blog", true), Ct(2, "News", true), Ct(3, "Draft", false) });

        var result = await Sut().GetAccessibleContentTypesAsync();

        Assert.Equal(new[] { 1, 2 }, result.Select(r => r.Id).ToArray());
        await _permissions.DidNotReceive().GetByUserAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Editor_sees_only_readable_published_types()
    {
        _user.IsAuthenticated.Returns(true);
        _user.IsAdmin.Returns(false);
        _user.UserId.Returns(7);
        _contentTypes.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<ContentType> { Ct(1, "Blog", true), Ct(2, "News", true) });
        _permissions.GetByUserAsync(7, Arg.Any<CancellationToken>())
            .Returns(new List<UserContentTypePermission>
            {
                new() { ContentTypeId = 1, CanRead = true },
                new() { ContentTypeId = 2, CanRead = false }
            });

        var result = await Sut().GetAccessibleContentTypesAsync();

        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
    }

    [Fact]
    public async Task Unauthenticated_sees_nothing()
    {
        _user.IsAuthenticated.Returns(false);
        Assert.Empty(await Sut().GetAccessibleContentTypesAsync());
    }

    [Fact]
    public async Task Admin_has_every_permission()
    {
        _user.IsAuthenticated.Returns(true);
        _user.IsAdmin.Returns(true);
        _user.UserId.Returns(1);
        Assert.True(await Sut().HasPermissionAsync(99, ContentPermission.Delete));
    }

    [Fact]
    public async Task Editor_permission_respects_flags()
    {
        _user.IsAuthenticated.Returns(true);
        _user.IsAdmin.Returns(false);
        _user.UserId.Returns(7);
        _permissions.GetByUserAsync(7, Arg.Any<CancellationToken>())
            .Returns(new List<UserContentTypePermission>
            {
                new() { ContentTypeId = 5, CanRead = true, CanCreate = true, CanUpdate = false, CanDelete = false }
            });

        var sut = Sut();
        Assert.True(await sut.HasPermissionAsync(5, ContentPermission.Read));
        Assert.True(await sut.HasPermissionAsync(5, ContentPermission.Create));
        Assert.False(await sut.HasPermissionAsync(5, ContentPermission.Update));
        Assert.False(await sut.HasPermissionAsync(5, ContentPermission.Delete));
        Assert.False(await sut.HasPermissionAsync(999, ContentPermission.Read));   // no row for this type
    }

    [Fact]
    public async Task GetPermissions_returns_full_flag_set_for_editor()
    {
        _user.IsAuthenticated.Returns(true);
        _user.IsAdmin.Returns(false);
        _user.UserId.Returns(7);
        _permissions.GetByUserAsync(7, Arg.Any<CancellationToken>())
            .Returns(new List<UserContentTypePermission>
            {
                new() { ContentTypeId = 5, CanRead = true, CanCreate = true, CanUpdate = true, CanDelete = false }
            });

        var set = await Sut().GetPermissionsAsync(5);
        Assert.True(set.CanRead);
        Assert.True(set.CanCreate);
        Assert.True(set.CanUpdate);
        Assert.False(set.CanDelete);
    }
}
