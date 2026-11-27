using System.ComponentModel.DataAnnotations.Schema;

namespace RouteRevisionHub.Models.Entities;

/// <summary>
/// Represents an end-of-chapter or custom timed exam (shared templates plus per-user custom exams).
/// </summary>
public class ChapterExam
{
    public const string TemplateOwner = "__TEMPLATE__";

    public int Id { get; set; }
    public string OwnerEmail { get; set; } = TemplateOwner;
    public int? TrackModuleId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string TitleAr { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int TimeLimitMinutes { get; set; } = 15;
    public int PassingScorePercent { get; set; } = 70;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Current user's deadline resolved from UserExamStates for Razor views.
    /// </summary>
    [NotMapped]
    public DateTime? DeadlineAt { get; set; }

    /// <summary>
    /// Current user's completion status resolved from UserExamStates for Razor views.
    /// </summary>
    [NotMapped]
    public bool IsCompleted { get; set; }

    /// <summary>
    /// Current user's best score resolved from UserExamStates for Razor views.
    /// </summary>
    [NotMapped]
    public int BestScorePercent { get; set; }

    public TrackModule? TrackModule { get; set; }
    public ICollection<FlashcardQuestion> CustomQuestions { get; set; } = new List<FlashcardQuestion>();
    public ICollection<ExamSubmission> Submissions { get; set; } = new List<ExamSubmission>();
    public ICollection<UserExamState> UserStates { get; set; } = new List<UserExamState>();
}
