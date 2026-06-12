using FluentValidation;
using Hemma.Modules.Property.Domain;

namespace Hemma.Modules.Property.Features.AreasTags;

internal sealed class PropertyAreaRequestValidator : AbstractValidator<PropertyAreaRequest>
{
    public PropertyAreaRequestValidator()
    {
        RuleFor(x => x.HouseholdId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Description).MaximumLength(1000);
    }
}

internal sealed class ReorderAreasRequestValidator : AbstractValidator<ReorderAreasRequest>
{
    public ReorderAreasRequestValidator()
    {
        RuleFor(x => x.HouseholdId).NotEmpty();
        RuleFor(x => x.AreaIds).NotEmpty();
    }
}

internal sealed class PropertyTagRequestValidator : AbstractValidator<PropertyTagRequest>
{
    public PropertyTagRequestValidator()
    {
        RuleFor(x => x.HouseholdId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Color).MaximumLength(40);
    }
}

internal sealed class AssignTagsRequestValidator : AbstractValidator<AssignTagsRequest>
{
    public AssignTagsRequestValidator()
    {
        RuleFor(x => x.HouseholdId).NotEmpty();
        RuleFor(x => x.TargetId).NotEmpty();
        RuleFor(x => x.TargetType).NotEmpty().Must(type =>
            Enum.TryParse<PropertyTagTargetType>(type, ignoreCase: true, out var parsed) && Enum.IsDefined(parsed));
        RuleFor(x => x.TagIds).NotNull();
    }
}
