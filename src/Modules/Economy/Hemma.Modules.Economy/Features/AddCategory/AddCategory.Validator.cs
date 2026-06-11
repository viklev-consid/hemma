using FluentValidation;

namespace Hemma.Modules.Economy.Features.AddCategory;

internal sealed class AddCategoryValidator : AbstractValidator<AddCategoryRequest>
{
    public AddCategoryValidator()
    {
        RuleFor(request => request.HouseholdId).NotEmpty();
        RuleFor(request => request.Name).NotEmpty().MaximumLength(100);
    }
}
