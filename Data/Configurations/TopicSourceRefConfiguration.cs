using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RouteRevisionHub.Models.Entities;

namespace RouteRevisionHub.Data.Configurations;

public class TopicSourceRefConfiguration : IEntityTypeConfiguration<TopicSourceRef>
{
    public void Configure(EntityTypeBuilder<TopicSourceRef> builder)
    {
        builder.ToTable("TopicSourceRefs");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.TopicId, x.CourseResourceId }).IsUnique();
        builder.Property(x => x.MatchReason).HasMaxLength(220);
        builder.Property(x => x.MatchedLocations).HasMaxLength(400);
        builder.Property(x => x.MatchedExcerpt).HasMaxLength(600);

        builder.HasOne(x => x.Topic)
            .WithMany(t => t.SourceRefs)
            .HasForeignKey(x => x.TopicId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.CourseResource)
            .WithMany(r => r.TopicRefs)
            .HasForeignKey(x => x.CourseResourceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
