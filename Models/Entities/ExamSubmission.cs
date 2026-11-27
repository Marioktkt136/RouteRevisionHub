namespace RouteRevisionHub.Models.Entities;

/// <summary>
/// Stores a per-user completed timed Chapter Exam attempt with full question-by-question answer breakdown.
/// </summary>
public class ExamSubmission
{
    public int Id { get; set; }
    public string UserEmail { get; set; } = string.Empty;
    public int ChapterExamId { get; set; }
    public int TotalQuestions { get; set; }
    public int CorrectAnswers { get; set; }
    public int ScorePercent { get; set; }
    public bool Passed { get; set; }
    public int DurationSecondsUsed { get; set; }
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public string AnswersBreakdownJson { get; set; } = "[]";

    public ChapterExam ChapterExam { get; set; } = null!;
}
