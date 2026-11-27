namespace RouteRevisionHub.Models.Entities;

/// <summary>
/// Links a curriculum topic to an exact course file or video, recording matched slides, pages, or timestamps.
/// </summary>
public class TopicSourceRef
{
    public int Id { get; set; }
    public int TopicId { get; set; }
    public int CourseResourceId { get; set; }
    public int RelevanceScore { get; set; }
    public string MatchReason { get; set; } = string.Empty;
    public string MatchedLocations { get; set; } = string.Empty;
    public int? FirstTimestampSeconds { get; set; }
    public string? MatchedExcerpt { get; set; }
    public bool IsPrimary { get; set; }

    public Topic Topic { get; set; } = null!;
    public CourseResource CourseResource { get; set; } = null!;
}
