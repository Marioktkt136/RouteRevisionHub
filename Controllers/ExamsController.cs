using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RouteRevisionHub.Data;
using RouteRevisionHub.Models.Entities;
using RouteRevisionHub.Services;

namespace RouteRevisionHub.Controllers;

public class ExamsIndexViewModel
{
    public List<ChapterExamCardVm> Exams { get; set; } = new();
    public List<ExamSubmission> RecentSubmissions { get; set; } = new();
    public List<TrackModule> Modules { get; set; } = new();
    public List<Topic> Topics { get; set; } = new();
}

public class ChapterExamCardVm
{
    public ChapterExam Exam { get; set; } = null!;
    public int AvailableQuestionsCount { get; set; }
    public string DeadlineBadge { get; set; } = "No Deadline";
    public string DeadlineBadgeClass { get; set; } = "bg-secondary";
}

public class TakeExamViewModel
{
    public ChapterExam Exam { get; set; } = null!;
    public List<ExamQuestionItemVm> Questions { get; set; } = new();
}

public class ExamQuestionItemVm
{
    public int Id { get; set; }
    public int TopicId { get; set; }
    public string TopicSlug { get; set; } = string.Empty;
    public string TopicTitle { get; set; } = string.Empty;
    public string QuestionType { get; set; } = "MCQ";
    public string Prompt { get; set; } = string.Empty;
    public string? CodeBlock { get; set; }
    public List<string> Options { get; set; } = new();
    public string CorrectAnswer { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
    public string SourceCitation { get; set; } = string.Empty;
}

public class ExamResultViewModel
{
    public ExamSubmission Submission { get; set; } = null!;
    public List<ExamAnswerBreakdownItem> Items { get; set; } = new();
}

public class ExamAnswerBreakdownItem
{
    public int QuestionId { get; set; }
    public int TopicId { get; set; }
    public string TopicSlug { get; set; } = string.Empty;
    public string TopicTitle { get; set; } = string.Empty;
    public string QuestionType { get; set; } = "MCQ";
    public string Prompt { get; set; } = string.Empty;
    public string SelectedAnswer { get; set; } = string.Empty;
    public string CorrectAnswer { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public string Explanation { get; set; } = string.Empty;
    public string SourceCitation { get; set; } = string.Empty;
}

[Authorize(Policy = "DriveGroupMember")]
public class ExamsController : Controller
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserAccessor _currentUser;

    public ExamsController(AppDbContext db, ICurrentUserAccessor currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IActionResult> Index()
    {
        string userEmail = _currentUser.Email;
        var modules = await _db.TrackModules.AsNoTracking().OrderBy(m => m.DisplayOrder).ToListAsync();
        var topics = await _db.Topics.AsNoTracking().Include(t => t.TrackModule)
            .OrderBy(t => t.TrackModule.DisplayOrder).ThenBy(t => t.DisplayOrder).ToListAsync();

        var exams = await _db.ChapterExams
            .AsNoTracking()
            .Include(e => e.TrackModule)
            .Include(e => e.CustomQuestions).ThenInclude(cq => cq.Topic)
            .Where(e => e.OwnerEmail == ChapterExam.TemplateOwner || e.OwnerEmail == userEmail)
            .OrderBy(e => e.TrackModule != null ? e.TrackModule.DisplayOrder : 999)
            .ThenBy(e => e.Id)
            .ToListAsync();

        var userStates = await _db.UserExamStates
            .AsNoTracking()
            .Where(s => s.UserEmail == userEmail)
            .ToDictionaryAsync(s => s.ChapterExamId);

        var qCountsByModule = await _db.FlashcardQuestions
            .AsNoTracking()
            .Include(q => q.Topic)
            .GroupBy(q => q.Topic.TrackModuleId)
            .Select(g => new { ModuleId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ModuleId, x => x.Count);

        int totalAllQuestions = qCountsByModule.Values.Sum();
        var today = DateTime.Today;

        var cards = exams.Select(e =>
        {
            if (userStates.TryGetValue(e.Id, out var st))
            {
                e.DeadlineAt = st.DeadlineAt;
                e.IsCompleted = st.IsCompleted;
                e.BestScorePercent = st.BestScorePercent;
            }

            int qCount = e.TrackModuleId.HasValue
                ? qCountsByModule.GetValueOrDefault(e.TrackModuleId.Value, 0) + e.CustomQuestions.Count(cq => cq.Topic?.TrackModuleId != e.TrackModuleId)
                : (e.CustomQuestions.Count > 0 ? e.CustomQuestions.Count : totalAllQuestions);

            string badge = "No Deadline";
            string cls = "bg-secondary";
            if (e.IsCompleted && e.BestScorePercent >= e.PassingScorePercent)
            {
                badge = $"Passed ({e.BestScorePercent}%)";
                cls = "bg-success";
            }
            else if (e.DeadlineAt.HasValue)
            {
                int days = (e.DeadlineAt.Value.Date - today).Days;
                if (days < 0)
                {
                    badge = $"Overdue by {-days}d";
                    cls = "bg-danger";
                }
                else if (days == 0)
                {
                    badge = "Due Today!";
                    cls = "bg-warning text-dark";
                }
                else if (days <= 3)
                {
                    badge = $"Due in {days}d";
                    cls = "bg-warning text-dark";
                }
                else
                {
                    badge = $"Due {e.DeadlineAt.Value:yyyy-MM-dd} ({days}d)";
                    cls = "bg-info text-dark";
                }
            }

            return new ChapterExamCardVm
            {
                Exam = e,
                AvailableQuestionsCount = qCount,
                DeadlineBadge = badge,
                DeadlineBadgeClass = cls
            };
        }).ToList();

        var submissions = await _db.ExamSubmissions
            .AsNoTracking()
            .Include(s => s.ChapterExam)
            .Where(s => s.UserEmail == userEmail)
            .OrderByDescending(s => s.SubmittedAt)
            .Take(15)
            .ToListAsync();

        return View(new ExamsIndexViewModel
        {
            Exams = cards,
            RecentSubmissions = submissions,
            Modules = modules,
            Topics = topics
        });
    }

    public async Task<IActionResult> Take(int id)
    {
        string userEmail = _currentUser.Email;
        var exam = await _db.ChapterExams
            .AsNoTracking()
            .Include(e => e.TrackModule)
            .FirstOrDefaultAsync(e => e.Id == id && (e.OwnerEmail == ChapterExam.TemplateOwner || e.OwnerEmail == userEmail));
        if (exam is null) return NotFound();

        var questionsQuery = _db.FlashcardQuestions
            .AsNoTracking()
            .Include(q => q.Topic)
            .AsQueryable();

        if (exam.TrackModuleId.HasValue)
        {
            questionsQuery = questionsQuery.Where(q =>
                q.ChapterExamId == exam.Id || q.Topic.TrackModuleId == exam.TrackModuleId.Value);
        }
        else
        {
            bool hasCustom = await _db.FlashcardQuestions.AnyAsync(q => q.ChapterExamId == exam.Id);
            if (hasCustom)
                questionsQuery = questionsQuery.Where(q => q.ChapterExamId == exam.Id);
        }

        var list = await questionsQuery
            .OrderBy(q => q.Topic.DisplayOrder)
            .ThenBy(q => q.Id)
            .Take(40)
            .ToListAsync();

        var vms = list.Select(q => new ExamQuestionItemVm
        {
            Id = q.Id,
            TopicId = q.TopicId,
            TopicSlug = q.Topic.Slug,
            TopicTitle = q.Topic.TitleEn,
            QuestionType = q.QuestionType,
            Prompt = q.Prompt,
            CodeBlock = q.CodeBlock,
            Options = string.IsNullOrWhiteSpace(q.OptionsJson)
                ? new()
                : (JsonSerializer.Deserialize<List<string>>(q.OptionsJson) ?? new()),
            CorrectAnswer = q.CorrectAnswer,
            Explanation = q.Explanation,
            SourceCitation = q.SourceCitation
        }).ToList();

        return View(new TakeExamViewModel
        {
            Exam = exam,
            Questions = vms
        });
    }

    public async Task<IActionResult> Result(int id)
    {
        string userEmail = _currentUser.Email;
        var submission = await _db.ExamSubmissions
            .AsNoTracking()
            .Include(s => s.ChapterExam).ThenInclude(e => e.TrackModule)
            .FirstOrDefaultAsync(s => s.Id == id && s.UserEmail == userEmail);
        if (submission is null) return NotFound();

        var breakdown = JsonSerializer.Deserialize<List<ExamAnswerBreakdownItem>>(
            submission.AnswersBreakdownJson,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();

        return View(new ExamResultViewModel
        {
            Submission = submission,
            Items = breakdown
        });
    }
}
