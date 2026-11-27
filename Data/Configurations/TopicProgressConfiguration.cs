using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RouteRevisionHub.Models.Entities;

namespace RouteRevisionHub.Data.Configurations;

public class TopicProgressConfiguration : IEntityTypeConfiguration<TopicProgress>
{
    public void Configure(EntityTypeBuilder<TopicProgress> builder)
    {
        builder.ToTable("TopicProgresses");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.UserEmail).IsRequired().HasMaxLength(180);
        builder.HasIndex(p => new { p.UserEmail, p.TopicId }).IsUnique();
        builder.Property(p => p.Status).IsRequired().HasMaxLength(24);

        builder.HasOne(p => p.Topic)
            .WithMany(t => t.Progresses)
            .HasForeignKey(p => p.TopicId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
