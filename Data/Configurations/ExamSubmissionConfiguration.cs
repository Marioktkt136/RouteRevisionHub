using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RouteRevisionHub.Models.Entities;

namespace RouteRevisionHub.Data.Configurations;

public class ExamSubmissionConfiguration : IEntityTypeConfiguration<ExamSubmission>
{
    public void Configure(EntityTypeBuilder<ExamSubmission> builder)
    {
        builder.ToTable("ExamSubmissions");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.UserEmail).IsRequired().HasMaxLength(180);
        builder.HasIndex(s => new { s.UserEmail, s.ChapterExamId });

        builder.HasOne(s => s.ChapterExam)
            .WithMany(e => e.Submissions)
            .HasForeignKey(s => s.ChapterExamId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
