namespace RouteRevisionHub.Models.Entities;

/// <summary>
/// Stores per-user exam deadline, completion state, and best score for a shared or custom ChapterExam.
/// </summary>
public class UserExamState
{
    public int Id { get; set; }
    public string UserEmail { get; set; } = string.Empty;
    public int ChapterExamId { get; set; }
    public DateTime? DeadlineAt { get; set; }
    public bool IsCompleted { get; set; }
    public int BestScorePercent { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ChapterExam ChapterExam { get; set; } = null!;
}
