using System.Security.Claims;
using henglong.Web.Common;
using henglong.Web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace henglong.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAdminAuthService _authService;

        public AccountController(IAdminAuthService authService)
        {
            _authService = authService;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View(new AdminLoginVm { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(AdminLoginVm model, CancellationToken ct)
        {
            var result = await _authService.AuthenticateAsync(model.Username, model.Password, ct);
            if (!result.Succeeded)
            {
                ViewData["ReturnUrl"] = model.ReturnUrl;
                ModelState.AddModelError(string.Empty, result.Message);
                return View(model);
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, result.Username),
                new(AdminClaimTypes.AdminType, result.AdminType)
            };
            if (result.DatabaseAdminId.HasValue)
            {
                claims.Add(new Claim(ClaimTypes.NameIdentifier, result.DatabaseAdminId.Value.ToString()));
            }

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity));

            var returnUrl = Url.IsLocalUrl(model.ReturnUrl) ? model.ReturnUrl : "/Product";
            return Redirect(returnUrl ?? "/Product");
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetAdminPassword([FromBody] AdminResetPasswordVm model, CancellationToken ct)
        {
            var result = await _authService.ResetDatabaseAdminPasswordAsync(User, model.Username, model.NewPassword, ct);
            return Json(new { success = result.Succeeded, message = result.Succeeded ? "成功" : result.Message });
        }
    }
}
