namespace RouteRevisionHub.Models.Entities;

/// <summary>
/// Represents an indexed course file or video stored on Google Drive (video, presentation, PDF, DOCX, SQL, code, archive).
/// </summary>
public class CourseResource
{
    public int Id { get; set; }
    public string RelativePath { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;
    public string ResourceType { get; set; } = string.Empty;
    public string ModuleFolder { get; set; } = string.Empty;
    public string Instructor { get; set; } = string.Empty;
    public string SessionName { get; set; } = string.Empty;
    public int DurationSeconds { get; set; }
    public string DurationFormatted { get; set; } = string.Empty;
    public decimal SizeMb { get; set; }
    public int PageOrSlideCount { get; set; }
    public string SearchableText { get; set; } = string.Empty;
    public string? StructuredPreviewJson { get; set; }

    public string? DriveFileId { get; set; }
    public string? DriveParentFolderId { get; set; }
    public string? DriveWebViewLink { get; set; }
    public string? DriveMimeType { get; set; }
    public string? DriveRootLabel { get; set; }

    public ICollection<TopicSourceRef> TopicRefs { get; set; } = new List<TopicSourceRef>();
}
