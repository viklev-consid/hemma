using Hemma.Modules.Economy.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Hemma.Modules.Economy.Persistence.Configurations;

internal sealed class CategorizationRuleConfiguration : IEntityTypeConfiguration<CategorizationRule>
{
    public void Configure(EntityTypeBuilder<CategorizationRule> builder)
    {
        builder.ToTable("categorization_rules");

        builder.HasKey(rule => rule.Id);

        builder.Property(rule => rule.Id)
            .HasConversion(id => id.Value, value => new CategorizationRuleId(value));

        builder.Property(rule => rule.HouseholdId)
            .IsRequired();

        builder.Property(rule => rule.Match)
            .HasConversion(match => match.Name, value => CategorizationRuleMatch.Create(value).Value)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(rule => rule.Pattern)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(rule => rule.TargetCategoryId)
            .HasConversion(id => id.Value, value => new CategoryId(value))
            .IsRequired();

        builder.Property(rule => rule.Enabled)
            .IsRequired();

        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(rule => rule.TargetCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(rule => rule.HouseholdId);
        builder.HasIndex(rule => new { rule.HouseholdId, rule.Match, rule.Pattern })
            .IsUnique();
    }
}
