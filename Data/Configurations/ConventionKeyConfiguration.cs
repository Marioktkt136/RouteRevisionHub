using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RouteRevisionHub.Models.Entities;

namespace RouteRevisionHub.Data.Configurations;

public class ConventionKeyConfiguration : IEntityTypeConfiguration<ConventionKey>
{
    public void Configure(EntityTypeBuilder<ConventionKey> builder)
    {
        builder.ToTable("ConventionKeys");
        builder.HasKey(k => k.Id);
        builder.Property(k => k.KeyName).IsRequired().HasMaxLength(150);
        builder.HasIndex(k => k.KeyName);
        builder.Property(k => k.Category).IsRequired().HasMaxLength(48);
        builder.Property(k => k.ShortRule).IsRequired().HasMaxLength(700);
        builder.Property(k => k.ExampleSnippet).HasMaxLength(500);

        builder.HasOne(k => k.Topic)
            .WithMany(t => t.ConventionKeys)
            .HasForeignKey(k => k.TopicId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
