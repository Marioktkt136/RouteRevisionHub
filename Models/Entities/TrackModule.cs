namespace RouteRevisionHub.Models.Entities;

/// <summary>
/// Represents a top-level curriculum module or learning track (e.g. C# Basics, OOP, MS SQL Server, EF Core, MVC).
/// </summary>
public class TrackModule
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public string TitleAr { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public string BadgeColor { get; set; } = "#38bdf8";
    public int TotalVideoCount { get; set; }
    public decimal TotalVideoHours { get; set; }
    public int TotalFileCount { get; set; }
    public bool IsUpcoming { get; set; }

    public ICollection<Topic> Topics { get; set; } = new List<Topic>();
}
