using VaryoCms.Application.Interfaces;
using VaryoCms.Application.Services;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace VaryoCms.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<DependencyInjectionMarker>();

        services.AddScoped<IContentTypeService, ContentTypeService>();
        services.AddScoped<IContentFieldService, ContentFieldService>();
        services.AddScoped<IContentItemService, ContentItemService>();
        services.AddScoped<IMediaService, MediaService>();
        services.AddScoped<IDictionaryService, DictionaryService>();
        services.AddScoped<ILanguageService, LanguageService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ISystemAuthService, SystemAuthService>();
        services.AddScoped<ISystemTenantService, SystemTenantService>();
        services.AddScoped<ISystemTranslationService, SystemTranslationService>();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<IApiConfigurationService, ApiConfigurationService>();
        services.AddScoped<IApiCredentialService, ApiCredentialService>();
        services.AddScoped<IPublicApiService, PublicApiService>();
        services.AddScoped<IAuditLogger, AuditLogger>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<ILoginCodeService, LoginCodeService>();
        services.AddScoped<IApiDocService, ApiDocService>();

        return services;
    }
}

// Anchor type for assembly scanning (validators).
internal sealed class DependencyInjectionMarker;
