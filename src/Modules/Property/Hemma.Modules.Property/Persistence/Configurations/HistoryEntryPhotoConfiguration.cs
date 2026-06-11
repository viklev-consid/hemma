using Hemma.Modules.Property.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hemma.Modules.Property.Persistence.Configurations;

internal sealed class HistoryEntryPhotoConfiguration : IEntityTypeConfiguration<HistoryEntryPhoto>
{
    public void Configure(EntityTypeBuilder<HistoryEntryPhoto> builder)
    {
        builder.ToTable("history_entry_photos");

        builder.HasKey(photo => new { photo.HistoryEntryId, photo.BlobKey });

        builder.Property(photo => photo.HistoryEntryId)
            .HasConversion(id => id.Value, value => new HistoryEntryId(value));

        builder.Property(photo => photo.BlobContainer)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(photo => photo.BlobKey)
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(photo => photo.FileName)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(photo => photo.ContentType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(photo => photo.Size)
            .IsRequired();
    }
}
