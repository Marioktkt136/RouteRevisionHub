using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RouteRevisionHub.Models.Entities;

namespace RouteRevisionHub.Data.Configurations;

public class TrackModuleConfiguration : IEntityTypeConfiguration<TrackModule>
{
    public void Configure(EntityTypeBuilder<TrackModule> builder)
    {
        builder.ToTable("TrackModules");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Code).IsRequired().HasMaxLength(24);
        builder.HasIndex(m => m.Code).IsUnique();
        builder.Property(m => m.TitleEn).IsRequired().HasMaxLength(160);
        builder.Property(m => m.TitleAr).IsRequired().HasMaxLength(160);
        builder.Property(m => m.Description).HasMaxLength(1000);
        builder.Property(m => m.BadgeColor).IsRequired().HasMaxLength(32);
        builder.Property(m => m.TotalVideoHours).HasColumnType("decimal(6,2)");
    }
}
