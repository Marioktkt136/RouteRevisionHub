using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace RouteRevisionHub.Services;

public class DriveRootFolderConfig
{
    public string Label { get; set; } = string.Empty;
    public string FolderId { get; set; } = string.Empty;
    public string Role { get; set; } = "MainTrack";
    public bool Required { get; set; }
}

public class GoogleDriveOptions
{
    public bool RequireAllRoots { get; set; }
    public List<DriveRootFolderConfig> RootFolders { get; set; } = new();
}

public record DriveAccessCheckResult(
    bool IsAllowed,
    List<string> AccessibleRootIds,
    List<string> AccessibleRootLabels,
    List<string> MissingRequiredLabels,
    string? ErrorMessage);

public class DriveApiFileDto
{
    [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("mimeType")] public string MimeType { get; set; } = string.Empty;
    [JsonPropertyName("size")] public string? SizeString { get; set; }
    [JsonPropertyName("modifiedTime")] public string? ModifiedTime { get; set; }
    [JsonPropertyName("webViewLink")] public string? WebViewLink { get; set; }
    [JsonPropertyName("shortcutDetails")] public DriveShortcutDto? ShortcutDetails { get; set; }

    [JsonIgnore]
    public long SizeBytes => long.TryParse(SizeString, out long s) ? s : 0;
}

public class DriveShortcutDto
{
    [JsonPropertyName("targetId")] public string? TargetId { get; set; }
    [JsonPropertyName("targetMimeType")] public string? TargetMimeType { get; set; }
}

file class DriveListResponseDto
{
    [JsonPropertyName("nextPageToken")] public string? NextPageToken { get; set; }
    [JsonPropertyName("files")] public List<DriveApiFileDto>? Files { get; set; }
}

file class OAuthRefreshResponseDto
{
    [JsonPropertyName("access_token")] public string? AccessToken { get; set; }
    [JsonPropertyName("expires_in")] public int ExpiresIn { get; set; }
    [JsonPropertyName("refresh_token")] public string? RefreshToken { get; set; }
}

/// <summary>
/// Wraps per-user Google Drive v3 API operations: strict folder ACL verification using the user's OWN OAuth token,
/// transparent token refresh, HTTP 206 byte-range video/file proxy streaming, and recursive folder listing.
/// </summary>
public class GoogleDriveService
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<GoogleDriveService> _logger;

    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public GoogleDriveService(
        IHttpClientFactory httpFactory,
        IConfiguration config,
        ILogger<GoogleDriveService> logger)
    {
        _httpFactory = httpFactory;
        _config = config;
        _logger = logger;
    }

    public GoogleDriveOptions GetDriveOptions()
    {
        var opts = new GoogleDriveOptions();
        _config.GetSection("GoogleDriveSettings").Bind(opts);
        return opts;
    }

    /// <summary>
    /// Verifies directly with Google Drive API using the logged-in user's OAuth token whether their Google account
    /// actually holds permission on the Route Group's Drive folder(s).
    /// </summary>
    public async Task<DriveAccessCheckResult> VerifyUserDriveAccessAsync(string accessToken, CancellationToken ct = default)
    {
        var opts = GetDriveOptions();
        if (opts.RootFolders.Count == 0)
            return new DriveAccessCheckResult(false, new(), new(), new(), "No GoogleDriveSettings:RootFolders configured.");

        var client = _httpFactory.CreateClient("GoogleDrive");
        var accessibleIds = new List<string>();
        var accessibleLabels = new List<string>();
        var missingRequired = new List<string>();
        string? lastErr = null;

        foreach (var root in opts.RootFolders)
        {
            if (string.IsNullOrWhiteSpace(root.FolderId)) continue;
            string url = $"https://www.googleapis.com/drive/v3/files/{Uri.EscapeDataString(root.FolderId)}?fields=id,name,mimeType&supportsAllDrives=true&includeItemsFromAllDrives=true";
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using var resp = await client.SendAsync(req, ct);
            if (resp.IsSuccessStatusCode)
            {
                accessibleIds.Add(root.FolderId);
                accessibleLabels.Add(root.Label);
            }
            else
            {
                lastErr = $"HTTP {(int)resp.StatusCode} on '{root.Label}'";
                if (root.Required || opts.RequireAllRoots)
                    missingRequired.Add(root.Label);
            }
        }

        bool allowed = accessibleIds.Count > 0 && missingRequired.Count == 0;
        return new DriveAccessCheckResult(
            allowed,
            accessibleIds,
            accessibleLabels,
            missingRequired,
            allowed ? null : (lastErr ?? "Your Google account does not have Reader/Member permission on the required course folders."));
    }

    public async Task<string?> GetValidUserAccessTokenAsync(HttpContext ctx, CancellationToken ct = default)
    {
        var auth = await ctx.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        if (!auth.Succeeded || auth.Properties is null)
            return null;

        string? accessToken = auth.Properties.GetTokenValue("access_token");
        string? refreshToken = auth.Properties.GetTokenValue("refresh_token");
        string? expiresAtRaw = auth.Properties.GetTokenValue("expires_at");

        bool needsRefresh = string.IsNullOrWhiteSpace(accessToken);
        if (!needsRefresh && DateTimeOffset.TryParse(expiresAtRaw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var exp))
        {
            if (exp <= DateTimeOffset.UtcNow.AddMinutes(2))
                needsRefresh = true;
        }

        if (!needsRefresh) return accessToken;
        if (string.IsNullOrWhiteSpace(refreshToken)) return accessToken;

        string? clientId = _config["Authentication:Google:ClientId"];
        string? clientSecret = _config["Authentication:Google:ClientSecret"];
        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
            return accessToken;

        try
        {
            var client = _httpFactory.CreateClient("GoogleOAuth");
            using var form = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret,
                ["refresh_token"] = refreshToken,
                ["grant_type"] = "refresh_token"
            });

            using var resp = await client.PostAsync("https://oauth2.googleapis.com/token", form, ct);
            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to refresh Google OAuth token: {Status}", resp.StatusCode);
                return accessToken;
            }

            string json = await resp.Content.ReadAsStringAsync(ct);
            var refreshed = JsonSerializer.Deserialize<OAuthRefreshResponseDto>(json, JsonOpts);
            if (refreshed is null || string.IsNullOrWhiteSpace(refreshed.AccessToken))
                return accessToken;

            var newExp = DateTimeOffset.UtcNow.AddSeconds(Math.Max(60, refreshed.ExpiresIn));
            auth.Properties.UpdateTokenValue("access_token", refreshed.AccessToken);
            auth.Properties.UpdateTokenValue("expires_at", newExp.ToString("o", CultureInfo.InvariantCulture));
            if (!string.IsNullOrWhiteSpace(refreshed.RefreshToken))
                auth.Properties.UpdateTokenValue("refresh_token", refreshed.RefreshToken);

            await ctx.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, auth.Principal!, auth.Properties);
            return refreshed.AccessToken;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error refreshing Google OAuth token.");
            return accessToken;
        }
    }

    /// <summary>
    /// Proxies a file/video download or HTTP 206 Byte-Range stream from Google Drive using the current user's OAuth token.
    /// Enforces real per-user Drive permissions at Google's API edge.
    /// </summary>
    public async Task<IActionResultProxyResult> ProxyStreamAsync(
        HttpContext ctx,
        string driveFileId,
        string fileName,
        string contentType,
        bool asDownload,
        CancellationToken ct)
    {
        string? token = await GetValidUserAccessTokenAsync(ctx, ct);
        if (string.IsNullOrWhiteSpace(token))
        {
            return new IActionResultProxyResult(StatusCodes.Status401Unauthorized,
                "Google OAuth access token is missing or expired. Please sign in again.");
        }

        var client = _httpFactory.CreateClient("GoogleDriveStream");
        string url = $"https://www.googleapis.com/drive/v3/files/{Uri.EscapeDataString(driveFileId)}?alt=media&supportsAllDrives=true";

        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (ctx.Request.Headers.TryGetValue("Range", out var rangeVals) && !string.IsNullOrWhiteSpace(rangeVals.ToString()))
        {
            req.Headers.TryAddWithoutValidation("Range", rangeVals.ToString());
        }

        using var driveResp = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);

        if (driveResp.StatusCode == HttpStatusCode.Unauthorized)
        {
            return new IActionResultProxyResult(StatusCodes.Status401Unauthorized,
                "Google Drive rejected the session token. Please sign out and sign in again.");
        }
        if (driveResp.StatusCode == HttpStatusCode.Forbidden || driveResp.StatusCode == HttpStatusCode.NotFound)
        {
            return new IActionResultProxyResult(StatusCodes.Status403Forbidden,
                "Your signed-in Google account does not have permission to view this Drive file.");
        }
        if (!driveResp.IsSuccessStatusCode && driveResp.StatusCode != HttpStatusCode.PartialContent)
        {
            return new IActionResultProxyResult((int)driveResp.StatusCode,
                $"Google Drive returned {(int)driveResp.StatusCode} ({driveResp.ReasonPhrase}).");
        }

        ctx.Response.StatusCode = (int)driveResp.StatusCode;
        ctx.Response.ContentType = contentType;
        ctx.Response.Headers["Accept-Ranges"] = "bytes";

        if (driveResp.Content.Headers.ContentRange is not null)
            ctx.Response.Headers["Content-Range"] = driveResp.Content.Headers.ContentRange.ToString();
        if (driveResp.Content.Headers.ContentLength.HasValue)
            ctx.Response.ContentLength = driveResp.Content.Headers.ContentLength.Value;

        string encodedName = Uri.EscapeDataString(fileName);
        ctx.Response.Headers["Content-Disposition"] = asDownload
            ? $"attachment; filename*=UTF-8''{encodedName}"
            : $"inline; filename*=UTF-8''{encodedName}";

        await driveResp.Content.CopyToAsync(ctx.Response.Body, ct);
        return new IActionResultProxyResult(ctx.Response.StatusCode, null, Streamed: true);
    }

    public async Task<List<DriveApiFileDto>> ListFolderChildrenAsync(
        string accessToken,
        string folderId,
        CancellationToken ct = default)
    {
        var client = _httpFactory.CreateClient("GoogleDrive");
        var all = new List<DriveApiFileDto>();
        string? pageToken = null;
        string q = Uri.EscapeDataString($"'{folderId}' in parents and trashed = false");
        const string fields = "nextPageToken,files(id,name,mimeType,size,modifiedTime,webViewLink,shortcutDetails)";

        do
        {
            string url = $"https://www.googleapis.com/drive/v3/files?q={q}&pageSize=200&fields={Uri.EscapeDataString(fields)}&supportsAllDrives=true&includeItemsFromAllDrives=true"
                       + (string.IsNullOrEmpty(pageToken) ? string.Empty : $"&pageToken={Uri.EscapeDataString(pageToken)}");

            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using var resp = await client.SendAsync(req, ct);
            if (resp.StatusCode == (HttpStatusCode)429 || resp.StatusCode == HttpStatusCode.Forbidden)
            {
                await Task.Delay(1200, ct);
                using var retryReq = new HttpRequestMessage(HttpMethod.Get, url);
                retryReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                using var retryResp = await client.SendAsync(retryReq, ct);
                retryResp.EnsureSuccessStatusCode();
                string retryJson = await retryResp.Content.ReadAsStringAsync(ct);
                var retryDto = JsonSerializer.Deserialize<DriveListResponseDto>(retryJson, JsonOpts);
                if (retryDto?.Files is not null) all.AddRange(retryDto.Files);
                pageToken = retryDto?.NextPageToken;
                continue;
            }

            resp.EnsureSuccessStatusCode();
            string json = await resp.Content.ReadAsStringAsync(ct);
            var dto = JsonSerializer.Deserialize<DriveListResponseDto>(json, JsonOpts);
            if (dto?.Files is not null) all.AddRange(dto.Files);
            pageToken = dto?.NextPageToken;
        }
        while (!string.IsNullOrEmpty(pageToken));

        return all;
    }

    public async Task<byte[]?> DownloadSmallFileBytesAsync(
        string accessToken,
        string driveFileId,
        long maxBytes = 15 * 1024 * 1024,
        CancellationToken ct = default)
    {
        var client = _httpFactory.CreateClient("GoogleDrive");
        string url = $"https://www.googleapis.com/drive/v3/files/{Uri.EscapeDataString(driveFileId)}?alt=media&supportsAllDrives=true";
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var resp = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
        if (!resp.IsSuccessStatusCode) return null;
        if (resp.Content.Headers.ContentLength > maxBytes) return null;

        await using var stream = await resp.Content.ReadAsStreamAsync(ct);
        using var ms = new MemoryStream();
        var buf = new byte[65536];
        int read;
        long total = 0;
        while ((read = await stream.ReadAsync(buf, ct)) > 0)
        {
            total += read;
            if (total > maxBytes) return null;
            ms.Write(buf, 0, read);
        }
        return ms.ToArray();
    }
}

public record IActionResultProxyResult(int StatusCode, string? ErrorMessage, bool Streamed = false);
