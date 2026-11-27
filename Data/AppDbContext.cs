using System.Reflection;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RouteRevisionHub.Models.Entities;

namespace RouteRevisionHub.Data;

/// <summary>
/// Application DbContext managing the shared Drive-backed curriculum graph, per-user progress/exams/checklists, and DataProtection keys.
/// </summary>
public class AppDbContext : DbContext, IDataProtectionKeyContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<TrackModule> TrackModules => Set<TrackModule>();
    public DbSet<Topic> Topics => Set<Topic>();
    public DbSet<ConventionKey> ConventionKeys => Set<ConventionKey>();
    public DbSet<CourseResource> CourseResources => Set<CourseResource>();
    public DbSet<TopicSourceRef> TopicSourceRefs => Set<TopicSourceRef>();
    public DbSet<FlashcardQuestion> FlashcardQuestions => Set<FlashcardQuestion>();
    public DbSet<TopicProgress> TopicProgresses => Set<TopicProgress>();
    public DbSet<QuizAttempt> QuizAttempts => Set<QuizAttempt>();
    public DbSet<ChecklistTask> ChecklistTasks => Set<ChecklistTask>();
    public DbSet<ChapterExam> ChapterExams => Set<ChapterExam>();
    public DbSet<UserExamState> UserExamStates => Set<UserExamState>();
    public DbSet<ExamSubmission> ExamSubmissions => Set<ExamSubmission>();
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
