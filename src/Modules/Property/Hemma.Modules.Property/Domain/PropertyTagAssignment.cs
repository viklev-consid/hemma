using ErrorOr;
using Hemma.Modules.Property.Errors;
using Hemma.Shared.Kernel.Domain;

namespace Hemma.Modules.Property.Domain;

public sealed class PropertyTagAssignment : AggregateRoot<PropertyTagAssignmentId>
{
    private PropertyTagAssignment(
        PropertyTagAssignmentId id,
        Guid householdId,
        PropertyTagId tagId,
        PropertyTagTargetType targetType,
        Guid targetId) : base(id)
    {
        HouseholdId = householdId;
        TagId = tagId;
        TargetType = targetType;
        TargetId = targetId;
    }

    private PropertyTagAssignment() : base(default!) { }

    public Guid HouseholdId { get; private set; }
    public PropertyTagId TagId { get; private set; }
    public PropertyTagTargetType TargetType { get; private set; }
    public Guid TargetId { get; private set; }

    public static ErrorOr<PropertyTagAssignment> Create(
        Guid householdId,
        PropertyTagId tagId,
        PropertyTagTargetType targetType,
        Guid targetId)
    {
        if (!Enum.IsDefined(targetType))
        {
            return PropertyErrors.TagTargetTypeInvalid;
        }

        return new PropertyTagAssignment(PropertyTagAssignmentId.New(), householdId, tagId, targetType, targetId);
    }
}
