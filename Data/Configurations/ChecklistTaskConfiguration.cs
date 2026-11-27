using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RouteRevisionHub.Models.Entities;

namespace RouteRevisionHub.Data.Configurations;

public class ChecklistTaskConfiguration : IEntityTypeConfiguration<ChecklistTask>
{
    public void Configure(EntityTypeBuilder<ChecklistTask> builder)
    {
        builder.ToTable("ChecklistTasks");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserEmail).IsRequired().HasMaxLength(180);
        builder.HasIndex(x => x.UserEmail);
        builder.Property(x => x.Title).IsRequired().HasMaxLength(240);
        builder.Property(x => x.Category).IsRequired().HasMaxLength(32);
        builder.Property(x => x.SourceHint).HasMaxLength(300);

        builder.HasOne(x => x.TrackModule)
            .WithMany()
            .HasForeignKey(x => x.TrackModuleId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
