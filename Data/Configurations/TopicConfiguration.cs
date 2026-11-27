using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RouteRevisionHub.Models.Entities;

namespace RouteRevisionHub.Data.Configurations;

public class TopicConfiguration : IEntityTypeConfiguration<Topic>
{
    public void Configure(EntityTypeBuilder<Topic> builder)
    {
        builder.ToTable("Topics");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Slug).IsRequired().HasMaxLength(140);
        builder.HasIndex(t => t.Slug).IsUnique();
        builder.Property(t => t.TitleEn).IsRequired().HasMaxLength(200);
        builder.Property(t => t.TitleAr).IsRequired().HasMaxLength(200);
        builder.Property(t => t.SessionLabel).HasMaxLength(100);
        builder.Property(t => t.Priority).IsRequired().HasMaxLength(32);
        builder.Property(t => t.CodeLanguage).IsRequired().HasMaxLength(24);

        builder.HasOne(t => t.TrackModule)
            .WithMany(m => m.Topics)
            .HasForeignKey(t => t.TrackModuleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
