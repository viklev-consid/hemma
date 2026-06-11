namespace Hemma.Modules.Households;

internal static class HouseholdsRoutes
{
    public const string GroupTag = "Households";
    public const string Prefix = "/v1/households";
    public const string MyHouseholds = $"{Prefix}/my";
    public const string ByRef = $"{Prefix}/{{householdRef}}";
    public const string Members = $"{ByRef}/members";
    public const string MemberByUserId = $"{Members}/{{userId:guid}}";
    public const string MemberRole = $"{MemberByUserId}/role";
    public const string Invitations = $"{ByRef}/invitations";
    public const string InvitationById = $"{Invitations}/{{invitationId:guid}}";
    public const string AcceptInvitation = $"{Prefix}/invitations/accept";
    public const string Audit = $"{ByRef}/audit";
}
