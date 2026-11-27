using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using RouteRevisionHub.Data;
using RouteRevisionHub.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();

builder.Services.AddHttpClient("GoogleDrive", c =>
{
    c.Timeout = TimeSpan.FromSeconds(45);
});
builder.Services.AddHttpClient("GoogleDriveStream", c =>
{
    c.Timeout = TimeSpan.FromMinutes(30);
});
builder.Services.AddHttpClient("GoogleOAuth", c =>
{
    c.Timeout = TimeSpan.FromSeconds(20);
});

var dbConn = DatabaseConnectionHelper.ResolveConnection(builder.Configuration);
builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (dbConn.IsPostgres)
        options.UseNpgsql(dbConn.ConnectionString, npg => npg.CommandTimeout(120));
    else
        options.UseSqlServer(dbConn.ConnectionString, sql => sql.CommandTimeout(120));
});

builder.Services.AddDataProtection()
    .PersistKeysToDbContext<AppDbContext>();

builder.Services.AddScoped<DatabaseSeeder>();
builder.Services.AddScoped<ICurrentUserAccessor, CurrentUserAccessor>();
builder.Services.AddScoped<GoogleDriveService>();
builder.Services.AddScoped<RevisionSearchService>();
builder.Services.AddScoped<CourseSyncService>();

var authBuilder = builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromDays(14);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

string? googleClientId = builder.Configuration["Authentication:Google:ClientId"];
string? googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
if (!string.IsNullOrWhiteSpace(googleClientId)
    && !string.IsNullOrWhiteSpace(googleClientSecret)
    && !googleClientId.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase))
{
    authBuilder.AddGoogle(options =>
    {
        options.ClientId = googleClientId;
        options.ClientSecret = googleClientSecret;
        options.AccessType = "offline";
        options.SaveTokens = true;
        options.CorrelationCookie.SameSite = SameSiteMode.Lax;
        options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("email");
        options.Scope.Add("https://www.googleapis.com/auth/drive.readonly");

        options.Events.OnRedirectToAuthorizationEndpoint = ctx =>
        {
            var log = ctx.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("GoogleOAuthRedirect");
            var uri = new Uri(ctx.RedirectUri);
            var qs = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uri.Query);
            log.LogWarning(
                "OAuth Redirect: ReqScheme={Scheme} ReqHost={Host} XFP={XFP} XFH={XFH} RedirectUri={RedirectUri} ClientId={ClientId}",
                ctx.Request.Scheme,
                ctx.Request.Host.Value,
                ctx.Request.Headers["X-Forwarded-Proto"].ToString(),
                ctx.Request.Headers["X-Forwarded-Host"].ToString(),
                qs.TryGetValue("redirect_uri", out var ru) ? ru.ToString() : "(none)",
                qs.TryGetValue("client_id", out var cid) ? cid.ToString() : "(none)");
            ctx.Response.Redirect(ctx.RedirectUri);
            return Task.CompletedTask;
        };

        options.Events.OnRemoteFailure = ctx =>
        {
            string errMsg = ctx.Failure?.Message ?? "Google authentication was cancelled or expired.";
            ctx.Response.Redirect("/Account/Login?oauthError=" + Uri.EscapeDataString(errMsg));
            ctx.HandleResponse();
            return Task.CompletedTask;
        };

        options.Events.OnCreatingTicket = async ctx =>
        {
            var driveSvc = ctx.HttpContext.RequestServices.GetRequiredService<GoogleDriveService>();
            var seeder = ctx.HttpContext.RequestServices.GetRequiredService<DatabaseSeeder>();

            string? accessToken = ctx.AccessToken;
            var identity = ctx.Principal?.Identity as ClaimsIdentity;
            if (identity is null || string.IsNullOrWhiteSpace(accessToken))
                return;

            string email = (ctx.Principal?.FindFirstValue(ClaimTypes.Email)
                ?? ctx.Principal?.FindFirstValue("email")
                ?? string.Empty).Trim().ToLowerInvariant();

            if (ctx.User.TryGetProperty("picture", out var picProp) && picProp.ValueKind == System.Text.Json.JsonValueKind.String)
                identity.AddClaim(new Claim("picture", picProp.GetString() ?? ""));

            var check = await driveSvc.VerifyUserDriveAccessAsync(accessToken, ctx.HttpContext.RequestAborted);
            if (check.IsAllowed)
            {
                identity.AddClaim(new Claim("drive_access", "granted"));
                identity.AddClaim(new Claim("drive_roots", string.Join(",", check.AccessibleRootIds)));
                identity.AddClaim(new Claim("drive_root_labels", string.Join(" · ", check.AccessibleRootLabels)));

                if (!string.IsNullOrWhiteSpace(email))
                    await seeder.EnsureUserSeededAsync(email, ctx.HttpContext.RequestAborted);
            }
            else
            {
                identity.AddClaim(new Claim("drive_access", "denied"));
                identity.AddClaim(new Claim("drive_denial_reason", check.ErrorMessage ?? "Google Drive permission check failed."));
            }
        };
    });
}

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("DriveGroupMember", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("drive_access", "granted");
    });
});

builder.Services.Configure<ForwardedHeadersOptions>(opts =>
{
    opts.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
    opts.ForwardLimit = null;
    opts.KnownIPNetworks.Clear();
    opts.KnownProxies.Clear();
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
    await seeder.SeedAsync();
}

app.UseForwardedHeaders();

string vercelProdHost = Environment.GetEnvironmentVariable("VERCEL_PROJECT_PRODUCTION_URL") ?? "route-revision-hub.vercel.app";
app.Use(async (ctx, next) =>
{
    string host = ctx.Request.Host.Host;
    if (host.StartsWith("route-revision-", StringComparison.OrdinalIgnoreCase)
        && host.EndsWith(".vercel.app", StringComparison.OrdinalIgnoreCase)
        && !string.Equals(host, vercelProdHost, StringComparison.OrdinalIgnoreCase)
        && !string.Equals(host, "route-revision-hub-mk-team11.vercel.app", StringComparison.OrdinalIgnoreCase))
    {
        string target = HttpMethods.IsGet(ctx.Request.Method) || HttpMethods.IsHead(ctx.Request.Method)
            ? $"https://{vercelProdHost}{ctx.Request.Path}{ctx.Request.QueryString}"
            : $"https://{vercelProdHost}/Account/Login";
        ctx.Response.Redirect(target, permanent: false);
        return;
    }
    await next();
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

public partial class Program { }
