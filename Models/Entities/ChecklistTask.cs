namespace RouteRevisionHub.Models.Entities;

/// <summary>
/// Represents an actionable course milestone, assignment, lab, exam, or custom study checklist item scoped per user or template.
/// </summary>
public class ChecklistTask
{
    public const string TemplateOwner = "__TEMPLATE__";

    public int Id { get; set; }
    public string UserEmail { get; set; } = TemplateOwner;
    public int? TrackModuleId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = "Custom";
    public string? SourceHint { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int DisplayOrder { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public TrackModule? TrackModule { get; set; }
}
