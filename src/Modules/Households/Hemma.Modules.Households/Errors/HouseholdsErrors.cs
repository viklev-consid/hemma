namespace Hemma.Modules.Households.Errors;

internal static class HouseholdsErrors
{
    public static readonly ErrorOr.Error HouseholdNotFound =
        ErrorOr.Error.NotFound("Households.Household.NotFound", "Household was not found.");

    public static readonly ErrorOr.Error HouseholdDeleted =
        ErrorOr.Error.Conflict("Households.Household.Deleted", "Household is deleted.");

    public static readonly ErrorOr.Error NameEmpty =
        ErrorOr.Error.Validation("Households.Name.Empty", "Household name cannot be empty.");

    public static readonly ErrorOr.Error NameTooLong =
        ErrorOr.Error.Validation("Households.Name.TooLong", "Household name cannot exceed 200 characters.");

    public static readonly ErrorOr.Error SlugEmpty =
        ErrorOr.Error.Validation("Households.Slug.Empty", "Household slug cannot be empty.");

    public static readonly ErrorOr.Error SlugInvalid =
        ErrorOr.Error.Validation("Households.Slug.Invalid", "Household slug must contain lowercase letters, numbers, and hyphens only.");

    public static readonly ErrorOr.Error SlugTooLong =
        ErrorOr.Error.Validation("Households.Slug.TooLong", "Household slug cannot exceed 100 characters.");

    public static readonly ErrorOr.Error SlugAlreadyExists =
        ErrorOr.Error.Conflict("Households.Slug.AlreadyExists", "Household slug is already in use.");

    public static readonly ErrorOr.Error MemberAlreadyExists =
        ErrorOr.Error.Conflict("Households.Member.AlreadyExists", "User is already a member of this household.");

    public static readonly ErrorOr.Error MemberNotFound =
        ErrorOr.Error.NotFound("Households.Member.NotFound", "Household member was not found.");

    public static readonly ErrorOr.Error LastOwnerRequired =
        ErrorOr.Error.Conflict("Households.Owner.LastOwnerRequired", "An active household must have at least one owner.");

    public static readonly ErrorOr.Error OwnedHouseholdsBlockUserErasure =
        ErrorOr.Error.Conflict("Households.Owner.UserErasureBlocked", "Transfer ownership or delete owned households before deleting this user.");

    public static readonly ErrorOr.Error RoleInvalid =
        ErrorOr.Error.Validation("Households.Role.Invalid", "Household role is not valid.");

    public static readonly ErrorOr.Error RoleEscalationForbidden =
        ErrorOr.Error.Forbidden("Households.Role.EscalationForbidden", "Cannot grant or modify an household role at or above your authority.");

    public static readonly ErrorOr.Error PlatformOverrideMutationForbidden =
        ErrorOr.Error.Forbidden("Households.PlatformOverride.MutationForbidden", "Platform override cannot mutate household state.");

    public static readonly ErrorOr.Error ConcurrencyConflict =
        ErrorOr.Error.Conflict("Households.Household.ConcurrencyConflict", "Household membership changed concurrently. Retry the operation.");

    public static readonly ErrorOr.Error PageSizeInvalid =
        ErrorOr.Error.Validation("Households.Query.PageSizeInvalid", $"Page size must be between 1 and {Shared.Kernel.Pagination.PageRequest.MaxPageSize}.");

    public static readonly ErrorOr.Error InvitationInvalid =
        ErrorOr.Error.Validation("Households.Invitation.Invalid", "Household invitation is invalid.");

    public static readonly ErrorOr.Error InvitationAlreadyAccepted =
        ErrorOr.Error.Conflict("Households.Invitation.AlreadyAccepted", "Household invitation has already been accepted.");

    public static readonly ErrorOr.Error InvitationAlreadyRevoked =
        ErrorOr.Error.Conflict("Households.Invitation.AlreadyRevoked", "Household invitation has already been revoked.");

    public static readonly ErrorOr.Error InvitationLifetimeInvalid =
        ErrorOr.Error.Validation("Households.Invitation.LifetimeInvalid", "Household invitation lifetime must be greater than zero.");
}
