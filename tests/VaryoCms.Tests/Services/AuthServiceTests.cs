using VaryoCms.Application.Common;
using VaryoCms.Application.Interfaces;
using VaryoCms.Application.Localization;
using VaryoCms.Application.Services;
using VaryoCms.Domain.Entities;
using VaryoCms.Domain.Enums;
using VaryoCms.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Localization;
using NSubstitute;

namespace VaryoCms.Tests.Services;

public class AuthServiceTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly ICurrentUserContext _currentUser = Substitute.For<ICurrentUserContext>();
    private readonly IStringLocalizer<SharedResource> _t = Substitute.For<IStringLocalizer<SharedResource>>();
    private readonly IAuditLogger _audit = Substitute.For<IAuditLogger>();

    public AuthServiceTests()
    {
        _t[Arg.Any<string>()].Returns(ci => new LocalizedString(ci.ArgAt<string>(0), ci.ArgAt<string>(0)));
    }

    private AuthService Sut() => new(_users, _hasher, _currentUser, _t, _audit);

    private User ActiveUser(string email = "user@test.com", string hash = "hash123") => new()
    {
        Id = 1, TenantId = 1, Email = email, PasswordHash = hash,
        FullName = "Test User", Role = UserRole.TenantAdmin, IsActive = true, IsDeleted = false
    };

    [Fact]
    public async Task ValidateCredentials_succeeds_with_correct_email_and_password()
    {
        var user = ActiveUser();
        _users.GetByEmailAsync("user@test.com", Arg.Any<CancellationToken>()).Returns(user);
        _hasher.Verify("correct-pass", "hash123").Returns(true);

        var result = await Sut().ValidateCredentialsAsync("user@test.com", "correct-pass");

        Assert.True(result.IsSuccess);
        Assert.Equal("user@test.com", result.Value!.Email);
        Assert.Equal(UserRole.TenantAdmin, result.Value.Role);
    }

    [Fact]
    public async Task ValidateCredentials_fails_for_wrong_password()
    {
        var user = ActiveUser();
        _users.GetByEmailAsync("user@test.com", Arg.Any<CancellationToken>()).Returns(user);
        _hasher.Verify("wrong-pass", "hash123").Returns(false);

        var result = await Sut().ValidateCredentialsAsync("user@test.com", "wrong-pass");

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task ValidateCredentials_fails_for_nonexistent_user()
    {
        _users.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await Sut().ValidateCredentialsAsync("nobody@test.com", "pass");

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task ValidateCredentials_fails_for_inactive_user()
    {
        var user = ActiveUser();
        user.IsActive = false;
        _users.GetByEmailAsync("user@test.com", Arg.Any<CancellationToken>()).Returns(user);

        var result = await Sut().ValidateCredentialsAsync("user@test.com", "pass");

        Assert.False(result.IsSuccess);
    }

    [Theory]
    [InlineData("", "pass")]
    [InlineData("email@x.com", "")]
    [InlineData(null, "pass")]
    public async Task ValidateCredentials_fails_for_empty_email_or_password(string? email, string? pass)
    {
        var result = await Sut().ValidateCredentialsAsync(email!, pass!);
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task ValidateCredentials_logs_audit_on_failure()
    {
        _users.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((User?)null);

        await Sut().ValidateCredentialsAsync("fail@test.com", "any");

        // NSubstitute arg matchers are positional — use Arg.Any for all params except action.
        await _audit.Received(1).LogAsync(
            AuditActions.LoginFailed,
            Arg.Any<string?>(),    // entityType
            Arg.Any<int?>(),       // entityId
            Arg.Any<int?>(),       // contentTypeId
            Arg.Any<string?>(),    // entityName
            Arg.Any<object?>(),    // metadata
            Arg.Any<string?>(),    // userEmailOverride
            Arg.Any<int?>(),       // userIdOverride
            Arg.Any<int?>(),       // tenantIdOverride
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FindByEmail_returns_dto_for_active_user()
    {
        _users.GetByEmailAsync("user@test.com", Arg.Any<CancellationToken>()).Returns(ActiveUser());

        var dto = await Sut().FindByEmailAsync("user@test.com");

        Assert.NotNull(dto);
        Assert.Equal("user@test.com", dto!.Email);
    }

    [Fact]
    public async Task FindByEmail_returns_null_for_inactive_user()
    {
        var user = ActiveUser();
        user.IsActive = false;
        _users.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(user);

        var dto = await Sut().FindByEmailAsync("user@test.com");

        Assert.Null(dto);
    }

    [Fact]
    public async Task ChangePassword_fails_if_not_authenticated()
    {
        _currentUser.UserId.Returns((int?)null);

        var result = await Sut().ChangePasswordAsync("old", "NewPass123!");

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task ChangePassword_fails_for_too_short_new_password()
    {
        _currentUser.UserId.Returns((int?)1);
        _users.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(ActiveUser());
        _hasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);

        var result = await Sut().ChangePasswordAsync("old", "short");

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task ChangePassword_fails_for_wrong_current_password()
    {
        _currentUser.UserId.Returns((int?)1);
        _users.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(ActiveUser());
        _hasher.Verify("wrong-old", "hash123").Returns(false);

        var result = await Sut().ChangePasswordAsync("wrong-old", "ValidNewPass123!");

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task ChangePassword_succeeds_and_updates_hash()
    {
        _currentUser.UserId.Returns((int?)1);
        _users.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(ActiveUser());
        _hasher.Verify("correct-old", "hash123").Returns(true);
        _hasher.Hash("ValidNewPass123!").Returns("new-hash");

        var result = await Sut().ChangePasswordAsync("correct-old", "ValidNewPass123!");

        Assert.True(result.IsSuccess);
        await _users.Received(1).UpdatePasswordAsync(1, "new-hash", Arg.Any<CancellationToken>());
    }
}
