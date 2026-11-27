using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RouteRevisionHub.Models.Entities;

namespace RouteRevisionHub.Data.Configurations;

public class FlashcardQuestionConfiguration : IEntityTypeConfiguration<FlashcardQuestion>
{
    public void Configure(EntityTypeBuilder<FlashcardQuestion> builder)
    {
        builder.ToTable("FlashcardQuestions");
        builder.HasKey(q => q.Id);
        builder.Property(q => q.QuestionType).IsRequired().HasMaxLength(24);
        builder.Property(q => q.Prompt).IsRequired().HasMaxLength(1200);
        builder.Property(q => q.CodeBlock).HasMaxLength(2000);
        builder.Property(q => q.OptionsJson).HasMaxLength(2000);
        builder.Property(q => q.CorrectAnswer).IsRequired().HasMaxLength(600);
        builder.Property(q => q.Explanation).HasMaxLength(1200);
        builder.Property(q => q.SourceCitation).HasMaxLength(300);

        builder.HasOne(q => q.Topic)
            .WithMany(t => t.Questions)
            .HasForeignKey(q => q.TopicId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(q => q.ChapterExam)
            .WithMany(e => e.CustomQuestions)
            .HasForeignKey(q => q.ChapterExamId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
