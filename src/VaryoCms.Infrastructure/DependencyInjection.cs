using Dapper;
using VaryoCms.Application.Common;
using VaryoCms.Application.Interfaces;
using VaryoCms.Domain.Interfaces.Repositories;
using VaryoCms.Infrastructure.Email;
using VaryoCms.Infrastructure.Media;
using VaryoCms.Infrastructure.Persistence;
using VaryoCms.Infrastructure.Persistence.Repositories;
using VaryoCms.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace VaryoCms.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ConfigureDapper();

        // Resolved lazily (via IServiceProvider) so WebApplicationFactory config overrides
        // (applied during ConfigureAppConfiguration) are picked up at first use, not captured
        // eagerly during service registration before the test host finishes building.
        services.AddSingleton<IDbConnectionFactory>(sp =>
        {
            IConfiguration cfg = sp.GetRequiredService<IConfiguration>();
            string cs = cfg.GetConnectionString("VaryoCms")
                ?? throw new InvalidOperationException("Connection string 'VaryoCms' is not configured.");
            return new DbConnectionFactory(cs);
        });

        // Same scoped instance behind both the concrete type (for repositories) and the interface (for services).
        services.AddScoped<UnitOfWork>();
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<UnitOfWork>());

        // Tenant resolution (runs before ITenantContext is set; no per-request state).
        services.AddSingleton<ITenantStore, TenantStore>();

        // Cross-tenant platform repositories — root tables, NO ITenantContext (no captive scoped dependency).
        services.AddSingleton<ISystemAdminRepository, SystemAdminRepository>();
        services.AddSingleton<ITenantProvisioningRepository, TenantProvisioningRepository>();
        services.AddSingleton<IUiTranslationRepository, UiTranslationRepository>();

        // DB-backed admin-UI localization (global). One factory feeds IViewLocalizer, IStringLocalizer<T>
        // and DataAnnotations localization. Registered after AddLocalization so this wins resolution.
        services.AddSingleton<IUiTranslationStore, VaryoCms.Infrastructure.Localization.UiTranslationStore>();
        services.AddSingleton<Microsoft.Extensions.Localization.IStringLocalizerFactory,
            VaryoCms.Infrastructure.Localization.DbStringLocalizerFactory>();

        // Repositories
        services.AddScoped<IContentTypeRepository, ContentTypeRepository>();
        services.AddScoped<IContentFieldRepository, ContentFieldRepository>();
        services.AddScoped<IContentItemRepository, ContentItemRepository>();
        services.AddScoped<IContentItemTitleRepository, ContentItemTitleRepository>();
        services.AddScoped<IContentFieldValueRepository, ContentFieldValueRepository>();
        services.AddScoped<IContentRelationRepository, ContentRelationRepository>();
        services.AddScoped<IMediaRepository, MediaRepository>();
        services.AddScoped<IDictionaryRepository, DictionaryRepository>();
        services.AddScoped<ILanguageRepository, LanguageRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserPermissionRepository, UserPermissionRepository>();
        services.AddScoped<IApiConfigurationRepository, ApiConfigurationRepository>();
        services.AddScoped<IApiCredentialRepository, ApiCredentialRepository>();
        services.AddScoped<IPublicApiRepository, PublicApiRepository>();
        services.AddScoped<IPublicApiWriteRepository, PublicApiWriteRepository>();
        services.AddScoped<IApiDocRepository, ApiDocRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IDashboardRepository, DashboardRepository>();
        services.AddScoped<ILoginCodeRepository, LoginCodeRepository>();
        services.AddSingleton<ITenantEmailSettingsRepository, TenantEmailSettingsRepository>();

        // Image inspection/processing (SixLabors.ImageSharp).
        services.AddSingleton<IImageProcessor, ImageProcessor>();

        // Password hashing (BCrypt).
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();

        // JWT issuing/validation for the public API (CMS-signed tokens). Resolved lazily for the
        // same reason as IDbConnectionFactory — so test config overrides are visible at first use.
        services.AddSingleton<IJwtTokenService>(sp =>
        {
            IConfiguration cfg = sp.GetRequiredService<IConfiguration>();
            string key = cfg["Jwt:SigningKey"]
                ?? throw new InvalidOperationException("'Jwt:SigningKey' is not configured.");
            string issuer = cfg["Jwt:Issuer"] ?? "VaryoCms";
            return new JwtTokenService(key, issuer);
        });

        // Public API response cache (backed by the app's IMemoryCache).
        services.AddSingleton<IResponseCache, VaryoCms.Infrastructure.Caching.MemoryResponseCache>();

        // Email verification (two-factor login). Settings bound from "EmailVerification" config section.
        var emailSettings = configuration.GetSection("EmailVerification").Get<EmailVerificationSettings>()
            ?? new EmailVerificationSettings();
        services.AddSingleton(emailSettings);
        services.AddSingleton<IEmailSender, SmtpEmailSender>();

        // Note: IFileStorageService is registered at the composition root (needs WebRootPath).

        return services;
    }

    private static void ConfigureDapper()
    {
        DefaultTypeMap.MatchNamesWithUnderscores = true;   // snake_case columns -> PascalCase properties
        // Enum columns are NVARCHAR. Reads: Dapper natively parses the name string back to the enum.
        // Writes: Dapper skips custom TypeHandlers for enum params, so repositories pass enum.ToString()
        // explicitly when inserting/updating (e.g. ContentFieldRepository.field_type).
    }
}
