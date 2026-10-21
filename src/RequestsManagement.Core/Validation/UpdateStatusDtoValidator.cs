using FluentValidation;
using RequestsManagement.Core.DTOs;

namespace RequestsManagement.Core.Validation;

public sealed class UpdateStatusDtoValidator : AbstractValidator<UpdateStatusDto>
{
    public UpdateStatusDtoValidator()
    {
        RuleFor(r => r.Status).IsInEnum();
        RuleFor(r => r.RowVersion).NotEmpty().Must(RowVersionFormat.IsValid).WithMessage(RowVersionFormat.ErrorMessage);
        RuleFor(r => r.ChangedBy).NotEmpty().MaximumLength(100);
    }
}
