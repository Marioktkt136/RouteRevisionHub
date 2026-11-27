using System.Security.Claims;

namespace RouteRevisionHub.Services;

public interface ICurrentUserAccessor
{
    string Email { get; }
    string DisplayName { get; }
    string? PictureUrl { get; }
    bool IsAuthenticated { get; }
    bool HasDriveAccess { get; }
    IReadOnlyList<string> AccessibleRootIds { get; }
}

/// <summary>
/// Reads normalized identity & verified Google Drive group claims from the current HttpContext user.
/// </summary>
public class CurrentUserAccessor : ICurrentUserAccessor
{
    private readonly IHttpContextAccessor _http;

    public CurrentUserAccessor(IHttpContextAccessor http) => _http = http;

    private ClaimsPrincipal? User => _http.HttpContext?.User;

    public bool IsAuthenticated =>
        User?.Identity?.IsAuthenticated == true && !string.IsNullOrWhiteSpace(Email);

    public string Email =>
        (User?.FindFirstValue(ClaimTypes.Email)
         ?? User?.FindFirstValue("email")
         ?? string.Empty).Trim().ToLowerInvariant();

    public string DisplayName =>
        User?.FindFirstValue(ClaimTypes.Name)
        ?? User?.FindFirstValue("name")
        ?? Email;

    public string? PictureUrl => User?.FindFirstValue("picture");

    public bool HasDriveAccess =>
        string.Equals(User?.FindFirstValue("drive_access"), "granted", StringComparison.OrdinalIgnoreCase);

    public IReadOnlyList<string> AccessibleRootIds
    {
        get
        {
            string raw = User?.FindFirstValue("drive_roots") ?? string.Empty;
            if (string.IsNullOrWhiteSpace(raw)) return Array.Empty<string>();
            return raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }
    }
}
