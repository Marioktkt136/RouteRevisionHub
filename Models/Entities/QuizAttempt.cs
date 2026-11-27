namespace RouteRevisionHub.Models.Entities;

/// <summary>
/// Records individual per-user flashcard or quiz attempts for accuracy analytics.
/// </summary>
public class QuizAttempt
{
    public int Id { get; set; }
    public string UserEmail { get; set; } = string.Empty;
    public int FlashcardQuestionId { get; set; }
    public int TopicId { get; set; }
    public string SelectedAnswer { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public DateTime AttemptedAt { get; set; } = DateTime.UtcNow;

    public FlashcardQuestion FlashcardQuestion { get; set; } = null!;
}
