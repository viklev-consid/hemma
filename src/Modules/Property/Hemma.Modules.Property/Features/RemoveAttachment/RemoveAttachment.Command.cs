namespace Hemma.Modules.Property.Features.RemoveAttachment;

public sealed record RemoveAttachmentCommand(Guid ProjectId, Guid AttachmentId, Guid HouseholdId);
