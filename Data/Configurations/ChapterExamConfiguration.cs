using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RouteRevisionHub.Models.Entities;

namespace RouteRevisionHub.Data.Configurations;

public class ChapterExamConfiguration : IEntityTypeConfiguration<ChapterExam>
{
    public void Configure(EntityTypeBuilder<ChapterExam> builder)
    {
        builder.ToTable("ChapterExams");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.OwnerEmail).IsRequired().HasMaxLength(180);
        builder.HasIndex(e => e.OwnerEmail);
        builder.Property(e => e.Title).IsRequired().HasMaxLength(200);
        builder.Property(e => e.TitleAr).HasMaxLength(200);
        builder.Property(e => e.Description).HasMaxLength(800);

        builder.HasOne(e => e.TrackModule)
            .WithMany()
            .HasForeignKey(e => e.TrackModuleId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
