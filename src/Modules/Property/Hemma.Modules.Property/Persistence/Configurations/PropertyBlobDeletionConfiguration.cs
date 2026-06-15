using Hemma.Modules.Property.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hemma.Modules.Property.Persistence.Configurations;

internal sealed class PropertyBlobDeletionConfiguration : IEntityTypeConfiguration<PropertyBlobDeletion>
{
    public void Configure(EntityTypeBuilder<PropertyBlobDeletion> builder)
    {
        builder.ToTable("pending_blob_deletions");

        builder.HasKey(deletion => deletion.Id);

        builder.Property(deletion => deletion.Id)
            .HasConversion(id => id.Value, value => new PropertyBlobDeletionId(value));

        builder.Property(deletion => deletion.HouseholdId)
            .IsRequired();

        builder.Property(deletion => deletion.BlobContainer)
            .HasMaxLength(ProjectAttachmentRules.MaxBlobContainerLength)
            .IsRequired();

        builder.Property(deletion => deletion.BlobKey)
            .HasMaxLength(ProjectAttachmentRules.MaxBlobKeyLength)
            .IsRequired();

        builder.Property(deletion => deletion.CreatedAt)
            .IsRequired();

        builder.Property(deletion => deletion.LastAttemptAt);

        builder.Property(deletion => deletion.AttemptCount)
            .IsRequired();

        builder.HasIndex(deletion => new { deletion.HouseholdId, deletion.CreatedAt });
        builder.HasIndex(deletion => new { deletion.BlobContainer, deletion.BlobKey }).IsUnique();
    }
}
