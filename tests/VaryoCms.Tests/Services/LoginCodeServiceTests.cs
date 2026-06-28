using VaryoCms.Application.Common;
using VaryoCms.Application.Interfaces;
using VaryoCms.Application.Services;
using VaryoCms.Domain.Entities;
using VaryoCms.Domain.Interfaces.Repositories;
using NSubstitute;

namespace VaryoCms.Tests.Services;

public class LoginCodeServiceTests
{
    private readonly ILoginCodeRepository _repo = Substitute.For<ILoginCodeRepository>();
    private readonly ITenantEmailSettingsRepository _tenantEmail = Substitute.For<ITenantEmailSettingsRepository>();
    private readonly IEmailSender _emailSender = Substitute.For<IEmailSender>();

    private readonly EmailVerificationSettings _settings = new()
    {
        Enabled = true,
        CodeExpiryMinutes = 5,
        MaxAttempts = 3,
        SmtpHost = "smtp.test.local",
        SmtpPort = 587,
        FromAddress = "no-reply@test.local"
    };

    private LoginCodeService Sut() => new(_repo, _tenantEmail, _emailSender, _settings);

    private LoginCode ActiveCode(string code = "123456", int attempts = 0) => new()
    {
        Id = 1,
        Email = "user@test.com",
        TenantType = "tenant",
        TenantId = 1,
        Code = code,
        Attempts = attempts,
        ExpiresAt = DateTime.UtcNow.AddMinutes(5),
        CreatedAt = DateTime.UtcNow
    };

    [Fact]
    public void IsSystemEnabled_reflects_settings()
    {
        Assert.True(Sut().IsSystemEnabled);

        var disabledSettings = new EmailVerificationSettings { Enabled = false };
        var sut = new LoginCodeService(_repo, _tenantEmail, _emailSender, disabledSettings);
        Assert.False(sut.IsSystemEnabled);
    }

    [Fact]
    public async Task IsTenantEnabled_returns_false_when_no_settings()
    {
        _tenantEmail.GetByTenantIdAsync(1, Arg.Any<CancellationToken>()).Returns((TenantEmailSettings?)null);

        bool enabled = await Sut().IsTenantEnabledAsync(1);

        Assert.False(enabled);
    }

    [Fact]
    public async Task IsTenantEnabled_returns_setting_value_when_configured()
    {
        _tenantEmail.GetByTenantIdAsync(1, Arg.Any<CancellationToken>()).Returns(new TenantEmailSettings
        {
            TenantId = 1, EmailVerificationEnabled = true,
            SmtpHost = "smtp.test.local", SmtpPort = 587,
            FromAddress = "no-reply@test.local", CodeExpiryMinutes = 5, MaxAttempts = 3
        });

        bool enabled = await Sut().IsTenantEnabledAsync(1);

        Assert.True(enabled);
    }

    [Fact]
    public async Task SendCode_calls_upsert_and_email_sender()
    {
        await Sut().SendCodeAsync("user@test.com", "tenant", 1);

        await _repo.Received(1).UpsertAsync(
            "user@test.com", "tenant", 1,
            Arg.Is<string>(code => code.Length == 6),
            Arg.Any<DateTime>(),
            Arg.Any<CancellationToken>());

        await _emailSender.Received(1).SendAsync(
            "user@test.com",
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<EmailVerificationSettings>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Verify_returns_failure_when_no_code_found()
    {
        _repo.GetActiveAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
             .Returns((LoginCode?)null);

        var result = await Sut().VerifyAsync("user@test.com", "tenant", 1, "123456");

        Assert.False(result.IsSuccess);
        Assert.Equal("NotFound", result.Error);
    }

    [Fact]
    public async Task Verify_returns_expired_when_code_is_past_expiry()
    {
        var expired = ActiveCode();
        expired.ExpiresAt = DateTime.UtcNow.AddMinutes(-1);  // expired
        _repo.GetActiveAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
             .Returns(expired);
        _tenantEmail.GetByTenantIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns((TenantEmailSettings?)null);

        var result = await Sut().VerifyAsync("user@test.com", "tenant", 1, "123456");

        Assert.False(result.IsSuccess);
        Assert.Equal("ExpiredCode", result.Error);
        await _repo.Received(1).DeleteByEmailAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Verify_returns_max_attempts_exceeded_when_too_many_tries()
    {
        var code = ActiveCode(attempts: 3);  // already at MaxAttempts (3)
        _repo.GetActiveAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
             .Returns(code);
        _tenantEmail.GetByTenantIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns((TenantEmailSettings?)null);

        var result = await Sut().VerifyAsync("user@test.com", "tenant", 1, "000000");

        Assert.False(result.IsSuccess);
        Assert.Equal("MaxAttemptsExceeded", result.Error);
    }

    [Fact]
    public async Task Verify_returns_invalid_code_error_with_remaining_attempts()
    {
        var code = ActiveCode("123456", attempts: 1);  // 1 attempt used, 2 remain after this
        _repo.GetActiveAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
             .Returns(code);
        _tenantEmail.GetByTenantIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns((TenantEmailSettings?)null);

        var result = await Sut().VerifyAsync("user@test.com", "tenant", 1, "999999");

        Assert.False(result.IsSuccess);
        Assert.StartsWith("InvalidCode:", result.Error);
        // remaining = maxAttempts(3) - attempts(1) - 1 = 1
        Assert.Equal("InvalidCode:1", result.Error);
        await _repo.Received(1).IncrementAttemptsAsync(1L, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Verify_succeeds_with_correct_code()
    {
        var code = ActiveCode("123456", attempts: 0);
        _repo.GetActiveAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
             .Returns(code);
        _tenantEmail.GetByTenantIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns((TenantEmailSettings?)null);

        var result = await Sut().VerifyAsync("user@test.com", "tenant", 1, "123456");

        Assert.True(result.IsSuccess);
        await _repo.Received(1).MarkUsedAsync(1L, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Verify_trims_code_before_comparison()
    {
        var code = ActiveCode("123456", attempts: 0);
        _repo.GetActiveAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
             .Returns(code);
        _tenantEmail.GetByTenantIdAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns((TenantEmailSettings?)null);

        // Code with leading/trailing spaces should still match after trim
        var result = await Sut().VerifyAsync("user@test.com", "tenant", 1, "  123456  ");

        Assert.True(result.IsSuccess);
    }
}
