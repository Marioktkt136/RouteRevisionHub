namespace RouteRevisionHub.Models.Entities;

/// <summary>
/// Represents a syntax rule, naming/mapping convention, keyword, or architectural key tied to a topic.
/// </summary>
public class ConventionKey
{
    public int Id { get; set; }
    public int TopicId { get; set; }
    public string KeyName { get; set; } = string.Empty;
    public string Category { get; set; } = "Syntax";
    public string ShortRule { get; set; } = string.Empty;
    public string? ExampleSnippet { get; set; }
    public bool IsCritical { get; set; }

    public Topic Topic { get; set; } = null!;
}
