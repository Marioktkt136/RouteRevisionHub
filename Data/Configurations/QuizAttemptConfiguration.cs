using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RouteRevisionHub.Models.Entities;

namespace RouteRevisionHub.Data.Configurations;

public class QuizAttemptConfiguration : IEntityTypeConfiguration<QuizAttempt>
{
    public void Configure(EntityTypeBuilder<QuizAttempt> builder)
    {
        builder.ToTable("QuizAttempts");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.UserEmail).IsRequired().HasMaxLength(180);
        builder.HasIndex(a => new { a.UserEmail, a.TopicId });
        builder.Property(a => a.SelectedAnswer).HasMaxLength(500);

        builder.HasOne(a => a.FlashcardQuestion)
            .WithMany()
            .HasForeignKey(a => a.FlashcardQuestionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
