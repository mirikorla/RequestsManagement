using FluentValidation;
using RequestsManagement.Core.DTOs;

namespace RequestsManagement.Core.Validation;

public sealed class RequestQueryDtoValidator : AbstractValidator<RequestQueryDto>
{
    public RequestQueryDtoValidator()
    {
        RuleFor(q => q.Page).GreaterThanOrEqualTo(1);
        RuleFor(q => q.PageSize).InclusiveBetween(1, RequestQueryDto.MaxPageSize);
        RuleFor(q => q.Search).MaximumLength(100);
        RuleFor(q => q.OrganizationName).MaximumLength(200);
        RuleFor(q => q.AssignedTo).MaximumLength(100);
        RuleFor(q => q.Status).IsInEnum();
        RuleFor(q => q.Priority).IsInEnum();
        RuleFor(q => q.SortBy).IsInEnum();
        RuleFor(q => q.SortDirection).IsInEnum();
        RuleFor(q => q)
            .Must(q => q.CreatedFrom is null || q.CreatedTo is null || q.CreatedFrom <= q.CreatedTo)
            .WithName("CreatedFrom")
            .WithMessage("'CreatedFrom' must be earlier than or equal to 'CreatedTo'.");
    }
}
