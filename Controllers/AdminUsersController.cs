using System.Security.Claims;
using henglong.Web.Common;
using henglong.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace henglong.Web.Controllers
{
    [Authorize]
    public class AdminUsersController : Controller
    {
        private readonly IAdminUserStore _store;
        private readonly PasswordHasher<AdminUser> _passwordHasher;

        public AdminUsersController(IAdminUserStore store, PasswordHasher<AdminUser> passwordHasher)
        {
            _store = store;
            _passwordHasher = passwordHasher;
        }

        [HttpGet]
        public IActionResult Index()
        {
            if (!IsSuperAdmin()) return Forbid();
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> List(CancellationToken ct)
        {
            if (!IsSuperAdmin()) return Forbid();

            var users = await _store.GetAllAsync(ct);
            return Json(users.Select(user => new AdminUserListItemDto
            {
                Id = user.Id,
                Username = user.Username,
                IsEnabled = user.IsEnabled,
                CreateTime = user.CreateTime,
                UpdateTime = user.UpdateTime,
                LastLoginTime = user.LastLoginTime
            }).ToList());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromBody] AdminUserCreateVm model, CancellationToken ct)
        {
            if (!IsSuperAdmin()) return Forbid();

            var username = model.Username.Trim();
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(model.Password))
            {
                return Json(new { success = false, message = "请输入管理员用户名和密码" });
            }

            var now = DateTime.Now;
            var admin = new AdminUser
            {
                Username = username,
                IsEnabled = true,
                CreateTime = now,
                UpdateTime = now
            };
            admin.PasswordHash = _passwordHasher.HashPassword(admin, model.Password);

            var created = await _store.CreateAsync(admin, ct);
            return Json(new { success = created, message = created ? "成功" : "管理员已存在" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetEnabled([FromBody] AdminUserStatusVm model, CancellationToken ct)
        {
            if (!IsSuperAdmin()) return Forbid();

            var username = model.Username.Trim();
            if (string.IsNullOrWhiteSpace(username))
            {
                return Json(new { success = false, message = "缺少管理员用户名" });
            }

            var changed = await _store.SetEnabledAsync(username, model.IsEnabled, ct);
            return Json(new { success = changed, message = changed ? "成功" : "管理员不存在" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword([FromBody] AdminResetPasswordVm model, CancellationToken ct)
        {
            if (!IsSuperAdmin()) return Forbid();

            var username = model.Username.Trim();
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(model.NewPassword))
            {
                return Json(new { success = false, message = "请输入管理员用户名和新密码" });
            }

            var admin = await _store.GetByUsernameAsync(username, ct);
            if (admin == null)
            {
                return Json(new { success = false, message = "管理员不存在" });
            }

            var hash = _passwordHasher.HashPassword(admin, model.NewPassword);
            var changed = await _store.ResetPasswordAsync(username, hash, ct);
            return Json(new { success = changed, message = changed ? "成功" : "重置失败" });
        }

        private bool IsSuperAdmin()
        {
            return string.Equals(
                User.FindFirstValue(AdminClaimTypes.AdminType),
                AdminIdentityTypes.Super,
                StringComparison.Ordinal);
        }
    }
}
