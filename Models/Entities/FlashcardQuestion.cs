namespace RouteRevisionHub.Models.Entities;

/// <summary>
/// Quick-review question or flashcard extracted from course Quick Checks, Question Banks, Exams, or LM NoteBooks.
/// </summary>
public class FlashcardQuestion
{
    public int Id { get; set; }
    public int TopicId { get; set; }
    public int? ChapterExamId { get; set; }
    public string QuestionType { get; set; } = "MCQ"; // MCQ, TrueFalse, Output, SQL, Interview
    public string Prompt { get; set; } = string.Empty;
    public string? CodeBlock { get; set; }
    public string? OptionsJson { get; set; }
    public string CorrectAnswer { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
    public string SourceCitation { get; set; } = string.Empty;

    public Topic Topic { get; set; } = null!;
    public ChapterExam? ChapterExam { get; set; }
}
