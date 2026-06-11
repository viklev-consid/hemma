using ErrorOr;
using Hemma.Modules.Property.Errors;

namespace Hemma.Modules.Property.Domain;

public sealed class HistoryEntryPhoto
{
    private HistoryEntryPhoto(
        HistoryEntryId historyEntryId,
        string blobContainer,
        string blobKey,
        string fileName,
        string contentType,
        long size)
    {
        HistoryEntryId = historyEntryId;
        BlobContainer = blobContainer;
        BlobKey = blobKey;
        FileName = fileName;
        ContentType = contentType;
        Size = size;
    }

    private HistoryEntryPhoto() { }

    public HistoryEntryId HistoryEntryId { get; private set; }
    public string BlobContainer { get; private set; } = string.Empty;
    public string BlobKey { get; private set; } = string.Empty;
    public string FileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long Size { get; private set; }

    public static ErrorOr<HistoryEntryPhoto> Create(
        HistoryEntryId historyEntryId,
        string blobContainer,
        string blobKey,
        string fileName,
        string contentType,
        long size)
    {
        if (!ProjectAttachmentRules.IsValidBlobReference(blobContainer, blobKey))
        {
            return PropertyErrors.HistoryPhotoBlobInvalid;
        }

        if (string.IsNullOrWhiteSpace(fileName) || fileName.Length > 255)
        {
            return PropertyErrors.HistoryPhotoInvalid;
        }

        if (!ProjectAttachmentRules.IsAllowed(contentType, size))
        {
            return PropertyErrors.HistoryPhotoInvalid;
        }

        return new HistoryEntryPhoto(
            historyEntryId,
            blobContainer.Trim(),
            blobKey.Trim(),
            fileName.Trim(),
            contentType.Trim(),
            size);
    }
}
