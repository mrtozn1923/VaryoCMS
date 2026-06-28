using VaryoCms.Application.DTOs.User;
using VaryoCms.Application.Localization;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace VaryoCms.Application.Validators.User;

public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    private readonly IStringLocalizer<SharedResource> _localizer;

    public CreateUserRequestValidator(IStringLocalizer<SharedResource> localizer)
    {
        _localizer = localizer;

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage(_ => _localizer["Validation.RequiredFv"])
            .EmailAddress().WithMessage(_ => _localizer["Validation.Email"])
            .MaximumLength(256);

        RuleFor(x => x.FullName).MaximumLength(200);

        RuleFor(x => x.Role).IsInEnum().WithMessage(_ => _localizer["Validation.Role"]);

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage(_ => _localizer["Validation.RequiredFv"])
            .MinimumLength(8).WithMessage(_ => _localizer["Validation.PasswordLength"])
            .MaximumLength(128);
    }
}
