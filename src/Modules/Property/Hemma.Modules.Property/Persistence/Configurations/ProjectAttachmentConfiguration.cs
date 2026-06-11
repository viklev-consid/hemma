using Hemma.Modules.Property.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hemma.Modules.Property.Persistence.Configurations;

internal sealed class ProjectAttachmentConfiguration : IEntityTypeConfiguration<ProjectAttachment>
{
    public void Configure(EntityTypeBuilder<ProjectAttachment> builder)
    {
        builder.ToTable("project_attachments");

        builder.HasKey(attachment => attachment.Id);

        builder.Property(attachment => attachment.Id)
            .HasConversion(id => id.Value, value => new ProjectAttachmentId(value));

        builder.Property(attachment => attachment.ProjectId)
            .HasConversion(id => id.Value, value => new ProjectId(value))
            .IsRequired();

        builder.Property(attachment => attachment.BlobContainer)
            .HasMaxLength(ProjectAttachmentRules.MaxBlobContainerLength)
            .IsRequired();

        builder.Property(attachment => attachment.BlobKey)
            .HasMaxLength(ProjectAttachmentRules.MaxBlobKeyLength)
            .IsRequired();

        builder.Property(attachment => attachment.FileName)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(attachment => attachment.ContentType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(attachment => attachment.Size)
            .IsRequired();
    }
}
