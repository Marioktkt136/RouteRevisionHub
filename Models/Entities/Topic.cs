using System.ComponentModel.DataAnnotations.Schema;

namespace RouteRevisionHub.Models.Entities;

/// <summary>
/// Represents a distinct revisable topic within a track module, including quick review notes and citations.
/// </summary>
public class Topic
{
    public int Id { get; set; }
    public int TrackModuleId { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public string TitleAr { get; set; } = string.Empty;
    public string SessionLabel { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public string Priority { get; set; } = "Core"; // InterviewCritical, Core, Advanced, Upcoming
    public string SummaryAr { get; set; } = string.Empty;
    public string SummaryEn { get; set; } = string.Empty;
    public string? CodeSnippet { get; set; }
    public string CodeLanguage { get; set; } = "csharp";
    public string CommonMistakes { get; set; } = string.Empty;
    public string InterviewQuestions { get; set; } = string.Empty;

    public TrackModule TrackModule { get; set; } = null!;
    public ICollection<ConventionKey> ConventionKeys { get; set; } = new List<ConventionKey>();
    public ICollection<TopicSourceRef> SourceRefs { get; set; } = new List<TopicSourceRef>();
    public ICollection<FlashcardQuestion> Questions { get; set; } = new List<FlashcardQuestion>();
    public ICollection<TopicProgress> Progresses { get; set; } = new List<TopicProgress>();

    /// <summary>
    /// Current user's TopicProgress when loaded with a per-user filtered Include.
    /// </summary>
    [NotMapped]
    public TopicProgress? Progress => Progresses.FirstOrDefault();
}
