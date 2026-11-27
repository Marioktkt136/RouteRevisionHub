namespace RouteRevisionHub.Models.Entities;

/// <summary>
/// Tracks persistent per-user revision state, confidence score, and custom notes per topic in MS SQL Server.
/// </summary>
public class TopicProgress
{
    public int Id { get; set; }
    public string UserEmail { get; set; } = string.Empty;
    public int TopicId { get; set; }
    public string Status { get; set; } = "NotStarted"; // NotStarted, InProgress, NeedsReview, Mastered
    public int ConfidenceLevel { get; set; } = 1; // 1..5
    public bool VideoWatched { get; set; }
    public bool SlidesReviewed { get; set; }
    public bool PracticeDone { get; set; }
    public string? UserNotes { get; set; }
    public int ReviewCount { get; set; }
    public DateTime? LastReviewedAt { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Topic Topic { get; set; } = null!;
}
