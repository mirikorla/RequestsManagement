using FluentValidation;
using RequestsManagement.Core.DTOs.Bulk;

namespace RequestsManagement.Core.Validation;

public sealed class BulkUpdateStatusDtoValidator : AbstractValidator<BulkUpdateStatusDto>
{
    public BulkUpdateStatusDtoValidator()
    {
        RuleFor(r => r.Status).IsInEnum();
        RuleFor(r => r.ChangedBy).NotEmpty().MaximumLength(100);
        RuleFor(r => r.Items).NotEmpty()
            .Must(items => items.Count <= BulkUpdateStatusDto.MaxItems)
            .WithMessage($"A bulk request can contain at most {BulkUpdateStatusDto.MaxItems} items.")
            .Must(items => items.Select(i => i.Id).Distinct().Count() == items.Count)
            .WithMessage("Each request id may appear only once.");
        RuleForEach(r => r.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.Id).GreaterThan(0);
            item.RuleFor(i => i.RowVersion).NotEmpty().Must(RowVersionFormat.IsValid).WithMessage(RowVersionFormat.ErrorMessage);
        });
    }
}
