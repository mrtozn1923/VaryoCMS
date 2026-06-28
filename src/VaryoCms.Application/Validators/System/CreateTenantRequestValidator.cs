using VaryoCms.Application.DTOs.System;
using VaryoCms.Application.Localization;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace VaryoCms.Application.Validators.System;

public class CreateTenantRequestValidator : AbstractValidator<CreateTenantRequest>
{
    private readonly IStringLocalizer<SharedResource> _localizer;

    public CreateTenantRequestValidator(IStringLocalizer<SharedResource> localizer)
    {
        _localizer = localizer;

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage(_ => _localizer["Validation.RequiredFv"])
            .MaximumLength(200);

        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage(_ => _localizer["Validation.RequiredFv"])
            .MaximumLength(100)
            .Matches("^[a-z0-9-]+$").WithMessage(_ => _localizer["Validation.TenantSlug"]);

        RuleFor(x => x.DefaultLanguageCode)
            .NotEmpty().WithMessage(_ => _localizer["Validation.RequiredFv"])
            .MaximumLength(5)
            .Matches("^[a-z]{2,5}$").WithMessage(_ => _localizer["Validation.LanguageCodeLower"]);

        RuleFor(x => x.DefaultLanguageName)
            .NotEmpty().WithMessage(_ => _localizer["Validation.RequiredFv"])
            .MaximumLength(100);

        RuleFor(x => x.FirstAdminEmail)
            .NotEmpty().WithMessage(_ => _localizer["Validation.RequiredFv"])
            .EmailAddress().WithMessage(_ => _localizer["Validation.Email"])
            .MaximumLength(256);

        RuleFor(x => x.FirstAdminFullName).MaximumLength(200);

        RuleFor(x => x.FirstAdminPassword)
            .NotEmpty().WithMessage(_ => _localizer["Validation.RequiredFv"])
            .MinimumLength(8).WithMessage(_ => _localizer["Validation.PasswordLength"])
            .MaximumLength(128);
    }
}
