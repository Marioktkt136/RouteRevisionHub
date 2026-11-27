using RouteRevisionHub.Models.Entities;

namespace RouteRevisionHub.ViewModels;

public class DashboardViewModel
{
    public int TotalModules { get; set; }
    public int TotalTopics { get; set; }
    public int MasteredTopics { get; set; }
    public int InProgressTopics { get; set; }
    public int NeedsReviewTopics { get; set; }
    public int TotalConventionKeys { get; set; }
    public int TotalQuestions { get; set; }
    public int TotalVideos { get; set; }
    public decimal TotalVideoHours { get; set; }
    public int TotalResources { get; set; }
    public int QuizCorrectCount { get; set; }
    public int QuizTotalCount { get; set; }

    public List<ModuleSummaryItem> Modules { get; set; } = new();
    public List<TopicCardItem> PriorityTopics { get; set; } = new();
    public List<TopicCardItem> WeakOrRecentTopics { get; set; } = new();
}

public class ModuleSummaryItem
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public string TitleAr { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string BadgeColor { get; set; } = "#38bdf8";
    public int TotalVideoCount { get; set; }
    public decimal TotalVideoHours { get; set; }
    public int TotalFileCount { get; set; }
    public bool IsUpcoming { get; set; }
    public int TopicCount { get; set; }
    public int MasteredCount { get; set; }
    public int ConventionKeyCount { get; set; }
    public int ProgressPercent => TopicCount == 0 ? 0 : (int)Math.Round(MasteredCount * 100.0 / TopicCount);
}

public class RoadmapViewModel
{
    public int? SelectedModuleId { get; set; }
    public string? SelectedStatus { get; set; }
    public string? SelectedPriority { get; set; }
    public string? Query { get; set; }
    public List<TrackModule> Modules { get; set; } = new();
}

public class TopicCardItem
{
    public int Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    public string TitleAr { get; set; } = string.Empty;
    public string ModuleTitleEn { get; set; } = string.Empty;
    public string ModuleCode { get; set; } = string.Empty;
    public string BadgeColor { get; set; } = "#38bdf8";
    public string SessionLabel { get; set; } = string.Empty;
    public string Priority { get; set; } = "Core";
    public string Status { get; set; } = "NotStarted";
    public int ConfidenceLevel { get; set; } = 1;
    public int KeyCount { get; set; }
    public int SourceCount { get; set; }
    public int VideoCount { get; set; }
    public int QuestionCount { get; set; }
    public List<string> TopKeys { get; set; } = new();
}

public class TopicDetailViewModel
{
    public Topic Topic { get; set; } = null!;
    public Topic? PreviousTopic { get; set; }
    public Topic? NextTopic { get; set; }
    public List<TopicSourceRef> VideoRefs { get; set; } = new();
    public List<TopicSourceRef> DocAndSlideRefs { get; set; } = new();
    public List<TopicSourceRef> CodeAndSqlRefs { get; set; } = new();
    public List<FlashcardViewItem> Flashcards { get; set; } = new();
}

public class FlashcardViewItem
{
    public int Id { get; set; }
    public string QuestionType { get; set; } = "MCQ";
    public string Prompt { get; set; } = string.Empty;
    public string? CodeBlock { get; set; }
    public List<string> Options { get; set; } = new();
    public string CorrectAnswer { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
    public string SourceCitation { get; set; } = string.Empty;
}

public class ResourcesLibraryViewModel
{
    public string? Query { get; set; }
    public string? ModuleFolder { get; set; }
    public string? ResourceType { get; set; }
    public string? Instructor { get; set; }
    public int Page { get; set; } = 1;
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public List<string> AllModules { get; set; } = new();
    public List<string> AllTypes { get; set; } = new();
    public List<string> AllInstructors { get; set; } = new();
    public List<CourseResource> Items { get; set; } = new();
}

public class ConventionGlossaryViewModel
{
    public string? Query { get; set; }
    public string? Category { get; set; }
    public bool CriticalOnly { get; set; }
    public List<string> Categories { get; set; } = new();
    public List<ConventionKey> Keys { get; set; } = new();
}

public class QuizArenaViewModel
{
    public int? ModuleId { get; set; }
    public string? QuestionType { get; set; }
    public List<TrackModule> Modules { get; set; } = new();
    public List<QuizQuestionRow> Questions { get; set; } = new();
}

public class QuizQuestionRow
{
    public int Id { get; set; }
    public int TopicId { get; set; }
    public string TopicSlug { get; set; } = string.Empty;
    public string TopicTitle { get; set; } = string.Empty;
    public string ModuleCode { get; set; } = string.Empty;
    public string QuestionType { get; set; } = string.Empty;
    public string Prompt { get; set; } = string.Empty;
    public string? CodeBlock { get; set; }
    public List<string> Options { get; set; } = new();
    public string CorrectAnswer { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
    public string SourceCitation { get; set; } = string.Empty;
}
