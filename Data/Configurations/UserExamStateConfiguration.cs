using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RouteRevisionHub.Models.Entities;

namespace RouteRevisionHub.Data.Configurations;

public class UserExamStateConfiguration : IEntityTypeConfiguration<UserExamState>
{
    public void Configure(EntityTypeBuilder<UserExamState> builder)
    {
        builder.ToTable("UserExamStates");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.UserEmail).IsRequired().HasMaxLength(180);
        builder.HasIndex(s => new { s.UserEmail, s.ChapterExamId }).IsUnique();

        builder.HasOne(s => s.ChapterExam)
            .WithMany(e => e.UserStates)
            .HasForeignKey(s => s.ChapterExamId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
