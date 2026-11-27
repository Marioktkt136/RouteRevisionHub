using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RouteRevisionHub.Migrations
{
    /// <inheritdoc />
    public partial class OnlineInitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CourseResources",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RelativePath = table.Column<string>(type: "nvarchar(420)", maxLength: 420, nullable: false),
                    FullPath = table.Column<string>(type: "nvarchar(520)", maxLength: 520, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(220)", maxLength: 220, nullable: false),
                    Extension = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    ResourceType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ModuleFolder = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Instructor = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    SessionName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    DurationSeconds = table.Column<int>(type: "int", nullable: false),
                    DurationFormatted = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    SizeMb = table.Column<decimal>(type: "decimal(8,2)", nullable: false),
                    PageOrSlideCount = table.Column<int>(type: "int", nullable: false),
                    SearchableText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StructuredPreviewJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DriveFileId = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    DriveParentFolderId = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    DriveWebViewLink = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    DriveMimeType = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    DriveRootLabel = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseResources", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DataProtectionKeys",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FriendlyName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Xml = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataProtectionKeys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TrackModules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    TitleEn = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    TitleAr = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    BadgeColor = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    TotalVideoCount = table.Column<int>(type: "int", nullable: false),
                    TotalVideoHours = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    TotalFileCount = table.Column<int>(type: "int", nullable: false),
                    IsUpcoming = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrackModules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChapterExams",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OwnerEmail = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    TrackModuleId = table.Column<int>(type: "int", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TitleAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(800)", maxLength: 800, nullable: false),
                    TimeLimitMinutes = table.Column<int>(type: "int", nullable: false),
                    PassingScorePercent = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChapterExams", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChapterExams_TrackModules_TrackModuleId",
                        column: x => x.TrackModuleId,
                        principalTable: "TrackModules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ChecklistTasks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserEmail = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    TrackModuleId = table.Column<int>(type: "int", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(240)", maxLength: 240, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    SourceHint = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChecklistTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChecklistTasks_TrackModules_TrackModuleId",
                        column: x => x.TrackModuleId,
                        principalTable: "TrackModules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Topics",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TrackModuleId = table.Column<int>(type: "int", nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(140)", maxLength: 140, nullable: false),
                    TitleEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TitleAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SessionLabel = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    Priority = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    SummaryAr = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SummaryEn = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CodeSnippet = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CodeLanguage = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    CommonMistakes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    InterviewQuestions = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Topics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Topics_TrackModules_TrackModuleId",
                        column: x => x.TrackModuleId,
                        principalTable: "TrackModules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExamSubmissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserEmail = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    ChapterExamId = table.Column<int>(type: "int", nullable: false),
                    TotalQuestions = table.Column<int>(type: "int", nullable: false),
                    CorrectAnswers = table.Column<int>(type: "int", nullable: false),
                    ScorePercent = table.Column<int>(type: "int", nullable: false),
                    Passed = table.Column<bool>(type: "bit", nullable: false),
                    DurationSecondsUsed = table.Column<int>(type: "int", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AnswersBreakdownJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamSubmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExamSubmissions_ChapterExams_ChapterExamId",
                        column: x => x.ChapterExamId,
                        principalTable: "ChapterExams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserExamStates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserEmail = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    ChapterExamId = table.Column<int>(type: "int", nullable: false),
                    DeadlineAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false),
                    BestScorePercent = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserExamStates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserExamStates_ChapterExams_ChapterExamId",
                        column: x => x.ChapterExamId,
                        principalTable: "ChapterExams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConventionKeys",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TopicId = table.Column<int>(type: "int", nullable: false),
                    KeyName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(48)", maxLength: 48, nullable: false),
                    ShortRule = table.Column<string>(type: "nvarchar(700)", maxLength: 700, nullable: false),
                    ExampleSnippet = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsCritical = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConventionKeys", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConventionKeys_Topics_TopicId",
                        column: x => x.TopicId,
                        principalTable: "Topics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FlashcardQuestions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TopicId = table.Column<int>(type: "int", nullable: false),
                    ChapterExamId = table.Column<int>(type: "int", nullable: true),
                    QuestionType = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    Prompt = table.Column<string>(type: "nvarchar(1200)", maxLength: 1200, nullable: false),
                    CodeBlock = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    OptionsJson = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CorrectAnswer = table.Column<string>(type: "nvarchar(600)", maxLength: 600, nullable: false),
                    Explanation = table.Column<string>(type: "nvarchar(1200)", maxLength: 1200, nullable: false),
                    SourceCitation = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlashcardQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FlashcardQuestions_ChapterExams_ChapterExamId",
                        column: x => x.ChapterExamId,
                        principalTable: "ChapterExams",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FlashcardQuestions_Topics_TopicId",
                        column: x => x.TopicId,
                        principalTable: "Topics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TopicProgresses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserEmail = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    TopicId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(24)", maxLength: 24, nullable: false),
                    ConfidenceLevel = table.Column<int>(type: "int", nullable: false),
                    VideoWatched = table.Column<bool>(type: "bit", nullable: false),
                    SlidesReviewed = table.Column<bool>(type: "bit", nullable: false),
                    PracticeDone = table.Column<bool>(type: "bit", nullable: false),
                    UserNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReviewCount = table.Column<int>(type: "int", nullable: false),
                    LastReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TopicProgresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TopicProgresses_Topics_TopicId",
                        column: x => x.TopicId,
                        principalTable: "Topics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TopicSourceRefs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TopicId = table.Column<int>(type: "int", nullable: false),
                    CourseResourceId = table.Column<int>(type: "int", nullable: false),
                    RelevanceScore = table.Column<int>(type: "int", nullable: false),
                    MatchReason = table.Column<string>(type: "nvarchar(220)", maxLength: 220, nullable: false),
                    MatchedLocations = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    FirstTimestampSeconds = table.Column<int>(type: "int", nullable: true),
                    MatchedExcerpt = table.Column<string>(type: "nvarchar(600)", maxLength: 600, nullable: true),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TopicSourceRefs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TopicSourceRefs_CourseResources_CourseResourceId",
                        column: x => x.CourseResourceId,
                        principalTable: "CourseResources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TopicSourceRefs_Topics_TopicId",
                        column: x => x.TopicId,
                        principalTable: "Topics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "QuizAttempts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserEmail = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    FlashcardQuestionId = table.Column<int>(type: "int", nullable: false),
                    TopicId = table.Column<int>(type: "int", nullable: false),
                    SelectedAnswer = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsCorrect = table.Column<bool>(type: "bit", nullable: false),
                    AttemptedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuizAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuizAttempts_FlashcardQuestions_FlashcardQuestionId",
                        column: x => x.FlashcardQuestionId,
                        principalTable: "FlashcardQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChapterExams_OwnerEmail",
                table: "ChapterExams",
                column: "OwnerEmail");

            migrationBuilder.CreateIndex(
                name: "IX_ChapterExams_TrackModuleId",
                table: "ChapterExams",
                column: "TrackModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_ChecklistTasks_TrackModuleId",
                table: "ChecklistTasks",
                column: "TrackModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_ChecklistTasks_UserEmail",
                table: "ChecklistTasks",
                column: "UserEmail");

            migrationBuilder.CreateIndex(
                name: "IX_ConventionKeys_KeyName",
                table: "ConventionKeys",
                column: "KeyName");

            migrationBuilder.CreateIndex(
                name: "IX_ConventionKeys_TopicId",
                table: "ConventionKeys",
                column: "TopicId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseResources_DriveFileId",
                table: "CourseResources",
                column: "DriveFileId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseResources_DriveParentFolderId",
                table: "CourseResources",
                column: "DriveParentFolderId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseResources_FileName",
                table: "CourseResources",
                column: "FileName");

            migrationBuilder.CreateIndex(
                name: "IX_CourseResources_Instructor",
                table: "CourseResources",
                column: "Instructor");

            migrationBuilder.CreateIndex(
                name: "IX_CourseResources_ModuleFolder",
                table: "CourseResources",
                column: "ModuleFolder");

            migrationBuilder.CreateIndex(
                name: "IX_CourseResources_RelativePath",
                table: "CourseResources",
                column: "RelativePath",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseResources_ResourceType",
                table: "CourseResources",
                column: "ResourceType");

            migrationBuilder.CreateIndex(
                name: "IX_ExamSubmissions_ChapterExamId",
                table: "ExamSubmissions",
                column: "ChapterExamId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamSubmissions_UserEmail_ChapterExamId",
                table: "ExamSubmissions",
                columns: new[] { "UserEmail", "ChapterExamId" });

            migrationBuilder.CreateIndex(
                name: "IX_FlashcardQuestions_ChapterExamId",
                table: "FlashcardQuestions",
                column: "ChapterExamId");

            migrationBuilder.CreateIndex(
                name: "IX_FlashcardQuestions_TopicId",
                table: "FlashcardQuestions",
                column: "TopicId");

            migrationBuilder.CreateIndex(
                name: "IX_QuizAttempts_FlashcardQuestionId",
                table: "QuizAttempts",
                column: "FlashcardQuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_QuizAttempts_UserEmail_TopicId",
                table: "QuizAttempts",
                columns: new[] { "UserEmail", "TopicId" });

            migrationBuilder.CreateIndex(
                name: "IX_TopicProgresses_TopicId",
                table: "TopicProgresses",
                column: "TopicId");

            migrationBuilder.CreateIndex(
                name: "IX_TopicProgresses_UserEmail_TopicId",
                table: "TopicProgresses",
                columns: new[] { "UserEmail", "TopicId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Topics_Slug",
                table: "Topics",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Topics_TrackModuleId",
                table: "Topics",
                column: "TrackModuleId");

            migrationBuilder.CreateIndex(
                name: "IX_TopicSourceRefs_CourseResourceId",
                table: "TopicSourceRefs",
                column: "CourseResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_TopicSourceRefs_TopicId_CourseResourceId",
                table: "TopicSourceRefs",
                columns: new[] { "TopicId", "CourseResourceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrackModules_Code",
                table: "TrackModules",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserExamStates_ChapterExamId",
                table: "UserExamStates",
                column: "ChapterExamId");

            migrationBuilder.CreateIndex(
                name: "IX_UserExamStates_UserEmail_ChapterExamId",
                table: "UserExamStates",
                columns: new[] { "UserEmail", "ChapterExamId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChecklistTasks");

            migrationBuilder.DropTable(
                name: "ConventionKeys");

            migrationBuilder.DropTable(
                name: "DataProtectionKeys");

            migrationBuilder.DropTable(
                name: "ExamSubmissions");

            migrationBuilder.DropTable(
                name: "QuizAttempts");

            migrationBuilder.DropTable(
                name: "TopicProgresses");

            migrationBuilder.DropTable(
                name: "TopicSourceRefs");

            migrationBuilder.DropTable(
                name: "UserExamStates");

            migrationBuilder.DropTable(
                name: "FlashcardQuestions");

            migrationBuilder.DropTable(
                name: "CourseResources");

            migrationBuilder.DropTable(
                name: "ChapterExams");

            migrationBuilder.DropTable(
                name: "Topics");

            migrationBuilder.DropTable(
                name: "TrackModules");
        }
    }
}
