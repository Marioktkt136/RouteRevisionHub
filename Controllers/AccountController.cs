using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RouteRevisionHub.Services;

namespace RouteRevisionHub.Controllers;

[AllowAnonymous]
public class AccountController : Controller
{
    private readonly IConfiguration _config;
    private readonly GoogleDriveService _drive;
    private readonly ICurrentUserAccessor _currentUser;

    public AccountController(IConfiguration config, GoogleDriveService drive, ICurrentUserAccessor currentUser)
    {
        _config = config;
        _drive = drive;
        _currentUser = currentUser;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null, string? oauthError = null)
    {
        if (_currentUser.IsAuthenticated && _currentUser.HasDriveAccess)
            return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : "/");

        if (_currentUser.IsAuthenticated && !_currentUser.HasDriveAccess)
            return RedirectToAction(nameof(AccessDenied));

        ViewBag.ReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : "/";
        ViewBag.OAuthError = oauthError;
        ViewBag.IsGoogleConfigured = IsGoogleOAuthConfigured();
        ViewBag.RootFolders = _drive.GetDriveOptions().RootFolders;
        ViewBag.CallbackUrl = $"{Request.Scheme}://{Request.Host}/signin-google";
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ExternalLogin(string? returnUrl = null, bool forceSelectAccount = false)
    {
        if (!IsGoogleOAuthConfigured())
            return RedirectToAction(nameof(Login), new { returnUrl });

        string safeReturn = Url.IsLocalUrl(returnUrl) ? returnUrl! : "/";
        var props = new GoogleChallengeProperties
        {
            RedirectUri = safeReturn,
            IsPersistent = true,
            AccessType = "offline",
            Prompt = forceSelectAccount ? "select_account consent" : "select_account"
        };

        return Challenge(props, GoogleDefaults.AuthenticationScheme);
    }

    [HttpGet]
    public IActionResult AccessDenied(string? reason = null)
    {
        ViewBag.Email = User?.FindFirstValue(ClaimTypes.Email) ?? User?.FindFirstValue("email") ?? "(Unknown)";
        ViewBag.Reason = reason ?? User?.FindFirstValue("drive_denial_reason")
            ?? "حسابك المسجل لا يملك صلاحية الوصول إلى مجلدات الجروب على Google Drive.";
        ViewBag.RootFolders = _drive.GetDriveOptions().RootFolders;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    private bool IsGoogleOAuthConfigured()
    {
        string? cid = _config["Authentication:Google:ClientId"];
        string? csec = _config["Authentication:Google:ClientSecret"];
        return !string.IsNullOrWhiteSpace(cid)
            && !string.IsNullOrWhiteSpace(csec)
            && !cid.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase);
    }
}
