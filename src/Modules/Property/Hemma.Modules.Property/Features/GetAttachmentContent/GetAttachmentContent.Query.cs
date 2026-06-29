namespace Hemma.Modules.Property.Features.GetAttachmentContent;

public sealed record GetAttachmentContentQuery(Guid ProjectId, Guid AttachmentId, Guid HouseholdId);
