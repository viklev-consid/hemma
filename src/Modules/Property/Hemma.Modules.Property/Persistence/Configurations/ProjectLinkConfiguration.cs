using Hemma.Modules.Property.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hemma.Modules.Property.Persistence.Configurations;

internal sealed class ProjectLinkConfiguration : IEntityTypeConfiguration<ProjectLink>
{
    public void Configure(EntityTypeBuilder<ProjectLink> builder)
    {
        builder.ToTable("project_links");

        builder.HasKey(link => link.Id);

        builder.Property(link => link.Id)
            .HasConversion(id => id.Value, value => new ProjectLinkId(value));

        builder.Property(link => link.ProjectId)
            .HasConversion(id => id.Value, value => new ProjectId(value))
            .IsRequired();

        builder.Property(link => link.Label)
            .HasMaxLength(160)
            .IsRequired();

        builder.Property(link => link.Url)
            .HasMaxLength(2048)
            .IsRequired();
    }
}
