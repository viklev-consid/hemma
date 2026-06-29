using ErrorOr;
using Hemma.Modules.Property.Errors;
using Hemma.Shared.Kernel.Domain;

namespace Hemma.Modules.Property.Domain;

public sealed class ProjectAttachment : Entity<ProjectAttachmentId>
{
    private ProjectAttachment(
        ProjectAttachmentId id,
        ProjectId projectId,
        string blobContainer,
        string blobKey,
        string fileName,
        string contentType,
        long size) : base(id)
    {
        ProjectId = projectId;
        BlobContainer = blobContainer;
        BlobKey = blobKey;
        FileName = fileName;
        ContentType = contentType;
        Size = size;
    }

    private ProjectAttachment() : base(default!) { }

    public ProjectId ProjectId { get; private set; } = null!;
    public string BlobContainer { get; private set; } = string.Empty;
    public string BlobKey { get; private set; } = string.Empty;
    public string FileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long Size { get; private set; }

    public static ErrorOr<ProjectAttachment> Create(
        ProjectId projectId,
        string blobContainer,
        string blobKey,
        string fileName,
        string contentType,
        long size)
    {
        if (!ProjectAttachmentRules.IsValidBlobReference(blobContainer, blobKey))
        {
            return PropertyErrors.AttachmentBlobInvalid;
        }

        if (!ProjectAttachmentRules.IsAllowed(contentType, size))
        {
            return PropertyErrors.AttachmentFileInvalid;
        }

        var normalizedFileName = fileName.Trim();
        if (normalizedFileName.Length is 0 or > 255)
        {
            return PropertyErrors.AttachmentFileInvalid;
        }

        return new ProjectAttachment(
            ProjectAttachmentId.New(),
            projectId,
            blobContainer.Trim(),
            blobKey.Trim(),
            normalizedFileName,
            contentType.Trim(),
            size);
    }
}
