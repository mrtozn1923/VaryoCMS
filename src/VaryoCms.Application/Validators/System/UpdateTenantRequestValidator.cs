using VaryoCms.Application.DTOs.System;
using VaryoCms.Application.Localization;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace VaryoCms.Application.Validators.System;

public class UpdateTenantRequestValidator : AbstractValidator<UpdateTenantRequest>
{
    private readonly IStringLocalizer<SharedResource> _localizer;

    public UpdateTenantRequestValidator(IStringLocalizer<SharedResource> localizer)
    {
        _localizer = localizer;

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage(_ => _localizer["Validation.RequiredFv"])
            .MaximumLength(200);
    }
}
