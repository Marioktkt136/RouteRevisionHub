namespace RouteRevisionHub.Data.Seed;

public class SeedPayloadDto
{
    public List<SeedModuleDto> Modules { get; set; } = new();
    public List<SeedResourceDto> Resources { get; set; } = new();
}

public class SeedModuleDto
{
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
    public List<SeedTopicDto> Topics { get; set; } = new();
}

public class SeedTopicDto
{
    public string Slug { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public string TitleAr { get; set; } = string.Empty;
    public string SessionLabel { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public string Priority { get; set; } = "Core";
    public string SummaryAr { get; set; } = string.Empty;
    public string SummaryEn { get; set; } = string.Empty;
    public string? CodeSnippet { get; set; }
    public string CodeLanguage { get; set; } = "csharp";
    public string CommonMistakes { get; set; } = string.Empty;
    public string InterviewQuestions { get; set; } = string.Empty;
    public List<SeedConventionKeyDto> ConventionKeys { get; set; } = new();
    public List<SeedSourceRefDto> SourceRefs { get; set; } = new();
    public List<SeedQuestionDto> Questions { get; set; } = new();
}

public class SeedConventionKeyDto
{
    public string KeyName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string ShortRule { get; set; } = string.Empty;
    public string? ExampleSnippet { get; set; }
    public bool IsCritical { get; set; }
}

public class SeedSourceRefDto
{
    public string ResourceRelativePath { get; set; } = string.Empty;
    public int RelevanceScore { get; set; }
    public string MatchReason { get; set; } = string.Empty;
    public string MatchedLocations { get; set; } = string.Empty;
    public int? FirstTimestampSeconds { get; set; }
    public string? MatchedExcerpt { get; set; }
    public bool IsPrimary { get; set; }
}

public class SeedQuestionDto
{
    public string QuestionType { get; set; } = "MCQ";
    public string Prompt { get; set; } = string.Empty;
    public string? CodeBlock { get; set; }
    public List<string> Options { get; set; } = new();
    public string CorrectAnswer { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
    public string SourceCitation { get; set; } = string.Empty;
}

public class SeedResourceDto
{
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
}
