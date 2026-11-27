using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RouteRevisionHub.Models.Entities;

namespace RouteRevisionHub.Data.Configurations;

public class CourseResourceConfiguration : IEntityTypeConfiguration<CourseResource>
{
    public void Configure(EntityTypeBuilder<CourseResource> builder)
    {
        builder.ToTable("CourseResources");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.RelativePath).IsRequired().HasMaxLength(420);
        builder.HasIndex(r => r.RelativePath).IsUnique();
        builder.Property(r => r.FullPath).IsRequired().HasMaxLength(520);
        builder.Property(r => r.FileName).IsRequired().HasMaxLength(220);
        builder.HasIndex(r => r.FileName);
        builder.Property(r => r.Extension).HasMaxLength(16);
        builder.Property(r => r.ResourceType).IsRequired().HasMaxLength(32);
        builder.HasIndex(r => r.ResourceType);
        builder.Property(r => r.ModuleFolder).IsRequired().HasMaxLength(80);
        builder.HasIndex(r => r.ModuleFolder);
        builder.Property(r => r.Instructor).IsRequired().HasMaxLength(80);
        builder.HasIndex(r => r.Instructor);
        builder.Property(r => r.SessionName).HasMaxLength(120);
        builder.Property(r => r.DurationFormatted).HasMaxLength(16);
        builder.Property(r => r.SizeMb).HasPrecision(8, 2);

        builder.Property(r => r.DriveFileId).HasMaxLength(120);
        builder.HasIndex(r => r.DriveFileId);
        builder.Property(r => r.DriveParentFolderId).HasMaxLength(120);
        builder.HasIndex(r => r.DriveParentFolderId);
        builder.Property(r => r.DriveWebViewLink).HasMaxLength(300);
        builder.Property(r => r.DriveMimeType).HasMaxLength(150);
        builder.Property(r => r.DriveRootLabel).HasMaxLength(80);
    }
}
