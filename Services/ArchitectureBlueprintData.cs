namespace RouteRevisionHub.Services;

public record MindMapEdgeDto(string FromSlug, string ToSlug, string Label, string Kind);

public record ProcessFlowDto(
    string Id,
    string TitleEn,
    string TitleAr,
    string Category,
    string AccentColor,
    string OverviewAr,
    List<ProcessStepDto> Steps,
    List<string> RelatedTopicSlugs);

public record ProcessStepDto(
    int StepNumber,
    string Title,
    string SubtitleAr,
    string Details,
    string? CodeSnippet,
    string? WarningNote);

/// <summary>
/// Supplies cross-chapter Topic prerequisite edges and detailed operational step-by-step blueprints.
/// </summary>
public static class ArchitectureBlueprintData
{
    public static List<MindMapEdgeDto> GetTopicDependencies() =>
    [
        // Cross-chapter conceptual prerequisites showing how topics branch into advanced layers
        new("web-apps-vs-websites-and-client-server", "http-https-tls-methods-and-status-codes", "Deepens HTTP/Client-Server", "prereq"),
        new("sdlc-phases-and-dotnet-track", "clr-value-vs-reference-stack-heap", "Begins C# Core", "prereq"),

        new("clr-value-vs-reference-stack-heap", "structs-vs-classes-encapsulation-access-modifiers", "Memory -> OOP Types", "branch"),
        new("clr-value-vs-reference-stack-heap", "collections-list-dictionary-hashset-bigo", "Heap/Hash Mechanics", "branch"),
        new("clr-value-vs-reference-stack-heap", "multithreading-lock-interlocked-deadlock", "Shared Heap Safety", "branch"),
        new("object-tostring-equals-boxing-casting", "generics-constraints-variance-equality", "Solves Boxing Overhead", "branch"),
        new("arrays-jagged-nullable-parameters", "linq-elements-aggregation-quantifiers", "Null Safety & Sequences", "branch"),

        new("structs-vs-classes-encapsulation-access-modifiers", "constructors-chaining-indexers-operator-overloading", "Object Construction", "prereq"),
        new("oop-inheritance-and-class-relationships", "polymorphism-virtual-override-new-binding", "Enables Inheritance Dispatch", "prereq"),
        new("polymorphism-virtual-override-new-binding", "abstraction-abstract-class-vs-interface", "Abstract / Contracts", "prereq"),
        new("abstraction-abstract-class-vs-interface", "capstone-oop-city-library-and-library-ef", "OOP System Design", "branch"),
        new("abstraction-abstract-class-vs-interface", "mvc-models-di-razor-views-and-layouts", "DI Interfaces (IRepo)", "branch"),

        new("delegates-action-func-predicate-events", "linq-foundations-var-dynamic-deferred-execution", "Lambdas Power LINQ", "branch"),
        new("extension-methods-anonymous-records-tuples", "linq-foundations-var-dynamic-deferred-execution", "Extension Pipeline + var", "branch"),
        new("linq-foundations-var-dynamic-deferred-execution", "linq-filtering-projection-ordering", "Standard Operators", "prereq"),
        new("linq-filtering-projection-ordering", "linq-partitioning-set-grouping-joins", "Grouping & Joins", "prereq"),
        new("linq-foundations-var-dynamic-deferred-execution", "efcore-architecture-orm-dbcontext-migrations", "IQueryable Translation", "branch"),
        new("linq-partitioning-set-grouping-joins", "mvc-iqueryable-vs-ienumerable-in-actions", "Remote vs Local Execution", "branch"),

        new("erd-conceptual-modeling-and-keys", "erd-to-relational-schema-mapping-rules", "8 Schema Rules", "prereq"),
        new("erd-to-relational-schema-mapping-rules", "normalization-1nf-2nf-3nf-and-denormalization", "Remove Redundancy", "prereq"),
        new("erd-to-relational-schema-mapping-rules", "efcore-relationships-mapping-and-delete-behavior", "Maps directly to EF FKs", "branch"),
        new("normalization-1nf-2nf-3nf-and-denormalization", "sql-server-storage-ddl-constraints-cascade", "Physical Tables", "prereq"),
        new("dml-select-filtering-null-top-offset-fetch", "sql-joins-inner-outer-cross-self-merge", "Multi-Table Queries", "prereq"),
        new("sql-joins-inner-outer-cross-self-merge", "grouping-aggregates-having-vs-where", "Aggregating Results", "prereq"),
        new("grouping-aggregates-having-vs-where", "window-ranking-functions-and-cte", "Row-Level Ranking + CTE", "prereq"),
        new("window-ranking-functions-and-cte", "temp-tables-vs-table-variables-vs-cte", "Intermediate Storage", "prereq"),
        new("indexes-clustered-nonclustered-covering-include", "views-indexed-views-schemabinding", "Query Performance", "prereq"),
        new("transactions-tcl-acid-error-handling", "efcore-crud-states-tracking-bulk", "SaveChanges Transaction", "branch"),

        new("efcore-architecture-orm-dbcontext-migrations", "efcore-config-conventions-annotations-fluent-api", "3-Tier Configuration", "prereq"),
        new("efcore-config-conventions-annotations-fluent-api", "efcore-relationships-mapping-and-delete-behavior", "Navigations & Delete Rules", "prereq"),
        new("efcore-relationships-mapping-and-delete-behavior", "efcore-loading-strategies-nplus1-split-query", "Include & N+1 Prevention", "prereq"),
        new("efcore-config-conventions-annotations-fluent-api", "efcore-inheritance-mapping-tph-tpt-tpc", "TPH / TPT / TPC", "prereq"),
        new("efcore-crud-states-tracking-bulk", "mvc-session-04", "Repository + DbContext", "branch"),
        new("async-await-task-whenall-cancellation", "efcore-crud-states-tracking-bulk", "Async SaveChanges / ToListAsync", "branch"),

        new("http-https-tls-methods-and-status-codes", "aspnet-core-project-types-kestrel-reverse-proxy", "Web Server Hosting", "prereq"),
        new("aspnet-core-project-types-kestrel-reverse-proxy", "mvc-pattern-controllers-and-iactionresult", "Kestrel -> MVC App", "prereq"),
        new("mvc-pattern-controllers-and-iactionresult", "mvc-middleware-request-pipeline-and-order", "Request Pipeline", "prereq"),
        new("mvc-middleware-request-pipeline-and-order", "mvc-routing-conventional-attribute-constraints", "Endpoint Dispatch", "prereq"),
        new("mvc-routing-conventional-attribute-constraints", "mvc-model-binding-and-validation", "Parameter Binding", "prereq"),
        new("mvc-model-binding-and-validation", "mvc-models-di-razor-views-and-layouts", "Views & DI", "prereq"),
        new("mvc-models-di-razor-views-and-layouts", "mvc-session-04", "Repository + DI Lifetimes + Edit PRG", "prereq"),
        new("mvc-session-04", "upcoming-web-api-rest-jwt-identity", "Next Track Phase", "future"),
        new("mvc-session-04", "upcoming-clean-architecture-patterns-deployment", "Layered Patterns", "future")
    ];

    public static List<ProcessFlowDto> GetProcessFlows() =>
    [
        new(
            "mvc-pipeline",
            "ASP.NET Core HTTP Request & Middleware Pipeline",
            "خطوات الـ HTTP Request من المتصفح لحد رجوع الـ HTML/JSON Response",
            "ASP.NET Core MVC",
            "#ec4899",
            "الرحلة الكاملة لأي Request بيدخل التطبيق في ASP.NET Core: من Reverse Proxy و Kestrel، مروراً بطبقات الـ Middleware بالترتيب، ثم الـ Routing والـ Model Binding والـ Controller والـ Razor Engine.",
            [
                new(1, "Client Request & Reverse Proxy", "1. المتصفح -> Reverse Proxy (Nginx / IIS)", "Browser sends HTTP/HTTPS request (Port 443 TLS). Reverse Proxy terminates HTTPS, handles caching/load balancing, and forwards to Kestrel.", "GET https://app.com/Plan/Edit/3 HTTP/1.1", "Never expose Kestrel directly without reverse proxy TLS/hardening in production."),
                new(2, "Kestrel Web Server (In-Process)", "2. سيرفر Kestrel يستقبل الطلب", "Built-in cross-platform managed web server inside your .NET app builds an HttpContext object.", "var app = builder.Build();\napp.Run();", "Kestrel runs inside your app's own OS process."),
                new(3, "Middleware Pipeline (Forward Pass)", "3. المرور على الـ Middlewares بالترتيب", "Every middleware registered via app.Use(...) executes in exact Program.cs order before calling await next().", "app.UseExceptionHandler(\"/Error\");\napp.UseStaticFiles();\napp.UseRouting();\napp.UseAuthentication();\napp.UseAuthorization();", "Wrong order breaks everything: UseAuthorization MUST follow UseRouting and UseAuthentication!"),
                new(4, "Routing & Endpoint Matching", "4. توجيه الرابط للـ Controller و Action", "Matches URL against pattern '{controller=Home}/{action=Index}/{id?}' or Attribute Routing + Route Constraints (:int, :min).", "app.MapControllerRoute(\"default\", \"{controller=Home}/{action=Index}/{id?}\");", "app.Run(...) earlier in the pipeline short-circuits and prevents reaching endpoints!"),
                new(5, "Model Binding & ModelState Validation", "5. تعبئة البارامترات والتحقق من الصحة", "Binds Form -> Route -> QueryString (or [FromBody]) into parameters/ViewModel and populates ModelState.", "if (!ModelState.IsValid) return View(vm);", "Always validate ViewModels on server; never trust client HTML/JS validation alone."),
                new(6, "Controller Action + Scoped DI + Repository/EF", "6. تنفيذ الـ Action وقراءة/حفظ الداتا", "DI container injects Scoped IPlanRepository / DbContext. Controller orchestrates query/update and picks IActionResult.", "var plan = await _repo.GetByIdAsync(id);\nreturn View(plan);", "Never inject a Scoped DbContext into a Singleton service (Captive Dependency!)."),
                new(7, "Razor Engine & Return Response", "7. تجميع الـ Razor View والرجوع عبر Pipeline", "Razor runs _ViewImports -> _ViewStart (Layout) -> Action.cshtml -> @RenderBody() inside _Layout. Response unwinds back through middlewares.", "Views/Plan/Edit.cshtml -> Views/Shared/_Layout.cshtml", "After POST success, use RedirectToAction (PRG) instead of returning View() directly.")
            ],
            ["mvc-middleware-request-pipeline-and-order", "mvc-routing-conventional-attribute-constraints", "mvc-model-binding-and-validation", "mvc-models-di-razor-views-and-layouts"]
        ),
        new(
            "mvc-edit-prg",
            "MVC Edit Form Flow (GET -> ViewModel -> POST + PRG)",
            "خطوات تعديل البيانات بأمان في MVC (Edit GET + Overposting Protection + POST/Redirect/Get)",
            "ASP.NET Core MVC",
            "#f472b6",
            "النمط المعتمد في MVC Session 03-04 لتعديل أي سجل في الداتابيز مع الحماية من Overposting و CSRF وتكرار الإرسال عند عمل Refresh.",
            [
                new(1, "Edit [HttpGet] Loads Current Data", "1. فتح الفورم ببيانات السجل الحالية", "User clicks Edit -> GET /Plan/Edit/3. Action validates id > 0, fetches entity, maps to EditPlanViewModel, returns View(vm).", "public async Task<IActionResult> Edit(int id)\n{\n    var p = await _repo.GetByIdAsync(id);\n    if (p is null) return NotFound();\n    return View(MapToVm(p));\n}", "Handle invalid/missing Id cleanly with NotFound() / BadRequest()."),
                new(2, "Razor Tag Helpers + AntiForgery Token", "2. رسم الفورم مع توكن الحماية CSRF", "<form asp-action=\"Edit\"> auto-generates hidden __RequestVerificationToken and fills inputs using asp-for.", "<form asp-action=\"Edit\" method=\"post\">\n  <input asp-for=\"Price\" />\n</form>", "Do NOT put admin-only/PK fields inside editable ViewModels."),
                new(3, "Edit [HttpPost] + [ValidateAntiForgeryToken]", "3. استقبال POST والتحقق من التوكن والبيانات", "Verifies Anti-Forgery cookie/token -> binds ONLY ViewModel properties -> runs [Range]/[Required] validation.", "[HttpPost, ValidateAntiForgeryToken]\npublic async Task<IActionResult> Edit([FromRoute] int id, EditPlanViewModel vm)", "Binding EF Entity directly instead of ViewModel exposes Overposting vulnerability!"),
                new(4, "ModelState Check & Failure Path", "4. لو فيه خطأ في المدخلات نرجع نفس الفورم", "If !ModelState.IsValid, return View(vm) directly so user sees validation errors without losing entered values.", "if (!ModelState.IsValid) return View(vm);", null),
                new(5, "Persist Changes + TempData + Redirect (PRG)", "5. الحفظ ثم Redirect لمنع تكرار الإرسال", "Copy allowed ViewModel fields -> SaveChangesAsync() -> store feedback in TempData -> RedirectToAction(Index).", "TempData[\"Success\"] = \"Plan updated!\";\nreturn RedirectToAction(nameof(Index));", "ViewData/ViewBag disappear across Redirect; only TempData survives a redirect!")
            ],
            ["mvc-session-04", "mvc-model-binding-and-validation"]
        ),
        new(
            "ef-query-pipeline",
            "EF Core LINQ Query Execution Pipeline",
            "كيف تتحول جملة LINQ في C# إلى SQL Server Query وتعود ككائنات C#",
            "Entity Framework Core",
            "#10b981",
            "رحلة الـ IQueryable من لحظة كتابة الشرط حتى ترجمته بـ Database Provider وتنفيذه في SQL Server وبناء الكائنات في الـ ChangeTracker.",
            [
                new(1, "Compose Deferred IQueryable<T>", "1. بناء الاستعلام المؤجل (لا يلمس الداتابيز)", "Calling .Where(), .OrderBy(), .Include(), .AsNoTracking() builds an Expression Tree in memory. Zero SQL runs yet.", "var q = _db.Orders.AsNoTracking()\n    .Include(o => o.Customer)\n    .Where(o => o.Total > 500);", "Calling .ToList() too early switches to IEnumerable and pulls the whole table into RAM!"),
                new(2, "Trigger Immediate Execution", "2. لحظة إطلاق التنفيذ الفعلي", "Calling .ToListAsync(), .FirstOrDefaultAsync(), .CountAsync(), or .AnyAsync() triggers execution.", "var list = await q.ToListAsync();", "Prefer .AnyAsync() over .CountAsync() > 0 to emit fast SQL EXISTS."),
                new(3, "Provider SQL Dialect Translation", "3. ترجمة Expression Tree إلى T-SQL", "Microsoft.EntityFrameworkCore.SqlServer compiles the Expression Tree into parameterized T-SQL SELECT with JOINs/WHERE.", "SELECT o.Id, c.Name FROM Orders o INNER JOIN Customers c ... WHERE o.Total > @__p_0", "Non-translatable C# helper methods throw client-evaluation errors."),
                new(4, "SQL Server Execution & Network Stream", "4. تنفيذ الاستعلام وإرجاع الصفوف", "SQL Server executes query plan (Seek/Scan) and streams result rows back to .NET.", null, "Multiple collection .Include() chains can cause Cartesian Explosion — add .AsSplitQuery()!"),
                new(5, "Materialization & Snapshot Tracking", "5. تحويل الصفوف لـ Entities (Tracked أو Detached)", "If Tracking (default): entities enter 'Unchanged' state with snapshots in ChangeTracker. If AsNoTracking(): returned 'Detached'.", "// Read-only views/APIs -> always AsNoTracking()\n// Modifying later? Keep Tracking ON", "Editing an AsNoTracking() entity + SaveChanges() saves NOTHING.")
            ],
            ["linq-foundations-var-dynamic-deferred-execution", "mvc-iqueryable-vs-ienumerable-in-actions", "efcore-loading-strategies-nplus1-split-query", "efcore-crud-states-tracking-bulk"]
        ),
        new(
            "ef-changetracker",
            "EF Core ChangeTracker & SaveChanges() State Machine",
            "دورة حياة حالات الكائن (Entity States) وكيف يقرر EF Core نوع SQL عند الحفظ",
            "Entity Framework Core",
            "#059669",
            "تحول أي كائن بين Detached و Added و Unchanged و Modified و Deleted وتأثير SaveChangesAsync() والعمليات السريعة ExecuteUpdate/Delete.",
            [
                new(1, "Create (Detached -> Added -> Unchanged)", "1. إضافة صف جديد", "new Product() starts Detached. _db.Add(p) marks Added. SaveChanges() emits INSERT and resets state to Unchanged.", "var p = new Product { Name = \"Mic\" };\n_db.Products.Add(p);\nawait _db.SaveChangesAsync();", "Add() alone inserts NOTHING into SQL Server until SaveChanges() runs!"),
                new(2, "Tracked Update (Unchanged -> Modified)", "2. تعديل ذكي للأعمدة المتغيرة فقط", "Load tracked entity -> change only p.Price -> ChangeTracker diffs snapshot and generates UPDATE setting Price ONLY.", "var p = await _db.Products.FindAsync(5);\np.Price = 199m;\nawait _db.SaveChangesAsync();", "Generates targeted UPDATE [Products] SET [Price] = @p0 WHERE [Id] = @p1."),
                new(3, "Disconnected Partial Update Pattern", "3. تحديث كائن مفصول من API/DTO بأمان", "Attach sparse object and explicitly mark ONLY modified properties true — NEVER call blind _db.Update(sparseDto)!", "_db.Attach(dto);\n_db.Entry(dto).Property(x => x.Price).IsModified = true;", "Blind _db.Update(dto) overwrites omitted columns with NULL / 0!"),
                new(4, "Delete (Unchanged -> Deleted -> Detached)", "4. حذف صف واستخدام Bulk Direct SQL", "_db.Remove(p) marks Deleted -> SaveChanges() sends DELETE. Or skip tracking completely with EF 7+ ExecuteDeleteAsync().", "await _db.Products.Where(p => p.IsArchived).ExecuteDeleteAsync();", "ExecuteUpdateAsync/ExecuteDeleteAsync execute immediately without calling SaveChanges()!")
            ],
            ["efcore-crud-states-tracking-bulk", "efcore-architecture-orm-dbcontext-migrations"]
        ),
        new(
            "ef-migrations",
            "EF Core Code First Migration & Precedence Workflow",
            "خطوات Code First من كتابة الـ Classes والـ Fluent API لحد تحديث جداول SQL Server",
            "Entity Framework Core",
            "#34d399",
            "كيفية بناء الموديل وتطبيق أولوية الإعدادات (Fluent API > Annotations > Conventions) ثم توليد الـ Migration وتنفيذها على السيرفر.",
            [
                new(1, "Model Entities + 3-Tier Config Resolution", "1. كتابة الكلاسات وتطبيق أولوية الإعدادات", "Start with POCO classes (Conventions: Id=PK, string=nvarchar(max)), add Data Annotations, and override via Fluent API (Winner!).", "builder.Property(p => p.Name).IsRequired().HasMaxLength(150);", "Priority: Fluent API > Data Annotations > Conventions."),
                new(2, "Register IEntityTypeConfiguration<T>", "2. فصل الإعدادات في كلاسات مستقلة", "Create dedicated config classes implementing IEntityTypeConfiguration<T> and scan assembly inside OnModelCreating.", "modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);", "Putting [Key] on two properties throws; composite keys require HasKey(x => new { x.A, x.B })."),
                new(3, "Generate Migration (Model vs Snapshot)", "3. توليد ملف الـ Migration بحساب الفرق", "Run Add-Migration / dotnet ef migrations add. CLI compares current model against *ModelSnapshot.cs to emit Up()/Down().", "dotnet ef migrations add InitialCreate", "Never delete migration files manually after updating DB — use Remove-Migration."),
                new(4, "Apply to SQL Server & Record History", "4. التنفيذ على الداتابيز وتسجيل التاريخ", "Update-Database runs only migrations missing from __EFMigrationsHistory inside a transaction and updates schema.", "dotnet ef database update", "Editing a model without running Add-Migration leaves the database unchanged!")
            ],
            ["efcore-architecture-orm-dbcontext-migrations", "efcore-config-conventions-annotations-fluent-api"]
        ),
        new(
            "sql-execution-order",
            "T-SQL Logical Query Execution & Paging Order",
            "الترتيب الحقيقي الذي ينفذ به SQL Server جملة الـ SELECT (ولماذا تفشل بعض الـ Aliases)",
            "MS SQL Server",
            "#0ea5e9",
            "تفصيل مراحل تنفيذ الاستعلام داخلياً في SQL Engine ولماذا WHERE تسبق SELECT و HAVING تأتي بعد GROUP BY.",
            [
                new(1, "FROM & JOIN ... ON", "1. تجميع الجداول ودمج العلاقات", "Identifies source tables, builds Cartesian pairs, filters by ON, and adds unmatched Outer rows (LEFT/RIGHT).", "FROM Employees e LEFT JOIN Departments d ON e.DeptId = d.Id", "Filtering right-table columns inside WHERE turns a LEFT JOIN into an INNER JOIN!"),
                new(2, "WHERE (Row-Level Filter)", "2. فلترة الصفوف قبل التجميع", "Evaluates predicates per raw row using three-valued logic (True/False/Unknown).", "WHERE e.IsActive = 1 AND e.ManagerId IS NOT NULL", "WHERE cannot reference SELECT aliases or aggregate functions like COUNT(*)."),
                new(3, "GROUP BY (Grouping Buckets)", "3. تقسيم الصفوف لمجموعات", "Collapses rows sharing identical GROUP BY keys into summary buckets.", "GROUP BY d.Name", "Every non-aggregated column in SELECT must exist identically in GROUP BY."),
                new(4, "HAVING (Group-Level Filter)", "4. فلترة المجموعات بعد التجميع", "Evaluates aggregate conditions across each formed group.", "HAVING COUNT(*) >= 5 AND AVG(e.Salary) > 8000", null),
                new(5, "SELECT, Window Functions & DISTINCT", "5. حساب الأعمدة ودوال الترتيب والـ Alias", "Evaluates column expressions, window rankings (ROW_NUMBER/DENSE_RANK OVER), aliases, and deduplication.", "SELECT d.Name, DENSE_RANK() OVER (ORDER BY AVG(e.Salary) DESC) AS Rnk", "Window functions cannot go directly inside WHERE — wrap in a CTE!"),
                new(6, "ORDER BY -> OFFSET-FETCH / TOP", "6. الترتيب والترقيم النهائي للصفحات", "Sorts final rows (SELECT aliases allowed here!), then slices page via OFFSET (@Page-1)*@Size ROWS FETCH NEXT @Size ROWS ONLY.", "ORDER BY AvgSalary DESC OFFSET 10 ROWS FETCH NEXT 10 ROWS ONLY;", "OFFSET-FETCH strictly requires an ORDER BY clause!")
            ],
            ["dml-select-filtering-null-top-offset-fetch", "sql-joins-inner-outer-cross-self-merge", "grouping-aggregates-having-vs-where", "window-ranking-functions-and-cte"]
        ),
        new(
            "db-design-pipeline",
            "Database Design: ERD -> 8 Mapping Rules -> Normalization -> Indexes",
            "رحلة تصميم قاعدة البيانات من الصفر: ERD ثم Mapping Rules ثم 1NF-3NF ثم الفهارس",
            "Database Design & SQL",
            "#14b8a6",
            "المنهجية الهندسية المتكاملة لتحويل متطلبات البيزنس إلى جداول سريعة ومحكومة بقيود سليمة.",
            [
                new(1, "Conceptual Design (ERD)", "1. رسم الـ ERD وتحديد الكيانات والـ Keys", "Identify Strong vs Weak Entities, attributes (Stored vs Derived), Candidate/Primary Keys, Cardinality (1:1/1:M/M:N), Total/Partial Participation.", null, "Do NOT store Derived columns like Age or Total when computable dynamically."),
                new(2, "Relational Schema (8 Mapping Rules)", "2. تطبيق قواعد التحويل الـ 8 لجداول", "1:M -> FK on Many side; M:N -> Junction table with Composite PK; 1:1 -> FK + UNIQUE on Total side; Weak Entity -> Parent PK + Partial Key.", "PRIMARY KEY (StudentId, CourseId)", "Forgetting UNIQUE on 1:1 FK quietly turns the relation into 1:M!"),
                new(3, "Normalization (1NF -> 2NF -> 3NF)", "3. التطبيع لمنع التكرار و Anomalies", "1NF: atomic cells; 2NF: remove partial dependencies on composite PK; 3NF: remove transitive dependencies (PK -> A -> B).", null, "Keep OLTP writes normalized to 3NF; apply intentional Denormalization only for read-heavy OLAP analytics."),
                new(4, "Physical Storage & B-Tree Indexing", "4. التخزين في 8KB Pages وبناء الفهارس", "1 Clustered Index sorts table data pages (.mdf); Non-Clustered Indexes accelerate lookups; add INCLUDE columns to eliminate Key Lookups.", "CREATE NONCLUSTERED INDEX IX_Orders_Status ON Orders(Status) INCLUDE (Total);", "Avoid wrapping indexed columns in functions in WHERE (non-SARGable forces Table Scan).")
            ],
            ["erd-conceptual-modeling-and-keys", "erd-to-relational-schema-mapping-rules", "normalization-1nf-2nf-3nf-and-denormalization", "indexes-clustered-nonclustered-covering-include"]
        ),
        new(
            "sql-tx-trigger",
            "SQL Transaction (ACID / WAL) & DML Trigger Execution",
            "خطوات تنفيذ الـ Transaction مع TRY...CATCH وتفعيل Triggers وجداول INSERTED/DELETED",
            "MS SQL Server",
            "#0284c7",
            "كيف يضمن SQL Server الـ ACID عبر Write-Ahead Logging (.ldf) ومتى تعمل Triggers مع جداول INSERTED و DELETED.",
            [
                new(1, "BEGIN TRANSACTION & Lock Acquisition", "1. فتح المعاملة وحجز الـ Locks", "BEGIN TRAN starts atomic work unit. SQL Server acquires row/page/table locks (Isolation) before mutating data.", "SET XACT_ABORT ON;\nBEGIN TRY BEGIN TRANSACTION;", null),
                new(2, "Write-Ahead Logging (.ldf) & DML", "2. تسجيل التغيير في الـ LDF ثم تنفيذ الـ DML", "WAL writes before/after images to .ldf transaction log FIRST for crash Durability, then modifies in-memory buffer pages.", "UPDATE Accounts SET Balance -= 500 WHERE Id = 1;", null),
                new(3, "Trigger Execution (INSERTED / DELETED)", "3. تشغيل AFTER / INSTEAD OF Triggers دفعة واحدة", "Populates virtual INSERTED (new rows) and DELETED (old rows) tables once per statement (multi-row safe!).", "INSERT INTO GrantAudit SELECT i.Id, d.Amount, i.Amount, SYSTEM_USER FROM INSERTED i JOIN DELETED d ON i.Id = d.Id;", "Never write scalar SELECT @id = Id FROM INSERTED — multi-row updates will lose rows!"),
                new(4, "COMMIT or CATCH + @@TRANCOUNT ROLLBACK", "4. تأكيد النجاح أو التراجع الكامل عند أي خطأ", "If all checks & constraints pass -> COMMIT TRAN. On error -> CATCH checks @@TRANCOUNT > 0 and rolls back completely.", "COMMIT TRAN;\nEND TRY\nBEGIN CATCH IF @@TRANCOUNT > 0 ROLLBACK TRAN; THROW; END CATCH", "Remember @TableVariable rows ignore ROLLBACK TRANSACTION, while #TempTable rows DO roll back.")
            ],
            ["transactions-tcl-acid-error-handling", "triggers-after-instead-of-inserted-deleted", "temp-tables-vs-table-variables-vs-cte"]
        ),
        new(
            "clr-oop-dispatch",
            "CLR Stack/Heap & OOP Static vs Dynamic Binding Resolution",
            "خطوات توزيع الميموري في الـ CLR وتحديد أي دالة تعمل وقت الـ Polymorphism (new vs override)",
            "C# & OOP Internals",
            "#818cf8",
            "فهم دقيق لكيفية تخصيص Stack/Heap وعند استدعاء دالة على Parent p = new Child() كيف يختار الـ CLR التنفيذ.",
            [
                new(1, "Allocate Value vs Reference in CLR", "1. التخصيص في Stack مقابل Heap", "Primitives & local structs store raw values on Stack frame. 'new Child()' allocates fields on Heap and pushes its 4/8-byte pointer onto Stack.", "Parent p = new Child();", "Struct fields inside a class live inside that class's Heap block!"),
                new(2, "Constructor Execution Order (:base first)", "2. ترتيب تشغيل الـ Constructors من الأب للابن", "CLR walks inheritance chain upward and ALWAYS executes Base constructor body before Derived constructor body.", "public Child() : base() { }", null),
                new(3, "Compile-Time Check (Method Signature)", "3. فحص الكومبايلر لصلاحية الاستدعاء", "Compiler checks whether Variable Type ('Parent') declares the method/property.", "p.Show(); // Parent must expose Show()", "Using 'object' causes Boxing if Value Type is assigned; Generics <T> avoid this."),
                new(4, "Dispatch Decision: new (Static) vs virtual/override (Dynamic)", "4. اختيار الميثود الفعلي (Early vs V-Table Late Binding)", "If base method is non-virtual or child uses 'new' -> runs Variable Type (Parent). If base is 'virtual' + child 'override' -> reads Heap object V-Table (Child)!", "p.NewHidden();   // Runs Parent!\np.Overridden();  // Runs Child!", "This V-Table runtime resolution is the core of Polymorphism and Dependency Inversion!")
            ],
            ["clr-value-vs-reference-stack-heap", "constructors-chaining-indexers-operator-overloading", "polymorphism-virtual-override-new-binding"]
        ),
        new(
            "concurrency-async",
            "Multithreading Sync vs Non-Blocking async/await Flow",
            "الفرق العملي بين قفل الـ Threads بمشاكل التزامن وبين تحرير الـ Thread عبر async/await",
            "Adv C# & Concurrency",
            "#a78bfa",
            "خطوات حماية البيانات المشتركة في الـ CPU Multithreading ومقارنتها بمسار تحرير ThreadPool في عمليات الـ I/O.",
            [
                new(1, "Identify CPU vs I/O Workload", "1. التمييز بين شغل CPU وشغل I/O (DB/Network)", "Parallel CPU calculations share Heap across threads (risking Race Conditions). Database/HTTP calls spend 99% of time waiting on external hardware.", null, "Never spawn raw new Thread() inside high-scale web actions — wastes ~1MB stack & freezes scalability."),
                new(2, "Sync Shared Heap State (CPU Path)", "2. حماية البيانات المشتركة (CPU-Bound)", "Protect multi-line sections with lock(_privateGate) in fixed lock order (prevents Deadlock!), or atomic single ops with Interlocked.Increment(ref x).", "lock (_gateA) { lock (_gateB) { /* fixed order */ } }", "Two threads acquiring Lock A->B vs Lock B->A creates Deadlock."),
                new(3, "Await Async Task (I/O-Bound Non-Blocking)", "3. استدعاء await وتحرير الـ Thread فوراً", "At 'await _db.ToListAsync()', current Thread returns immediately to ThreadPool to serve other users while OS I/O runs.", "var results = await Task.WhenAll(t1, t2);", "Calling .Result or .Wait() blocks the thread (Sync-over-Async) causing ThreadPool Starvation!"),
                new(4, "Continuation Resumes cleanly", "4. استكمال التنفيذ عند جاهزية النتيجة", "When I/O finishes, a ThreadPool thread picks up the continuation after 'await' and returns the final Task<T>.", "return Ok(results);", null)
            ],
            ["multithreading-lock-interlocked-deadlock", "async-await-task-whenall-cancellation"]
        )
    ];
}
