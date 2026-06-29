namespace Hemma.Modules.Property.Features.AddAttachment;

public sealed record AddAttachmentCommand(Guid ProjectId, Guid HouseholdId, string FileName, string ContentType, byte[] Content);
