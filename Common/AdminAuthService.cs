using System.Security.Claims;
using henglong.Web.Models;
using Microsoft.AspNetCore.Identity;

namespace henglong.Web.Common
{
    public class AdminAuthService : IAdminAuthService
    {
        private const string EmptyCredentialsMessage = "请输入用户名和密码";
        private const string InvalidCredentialsMessage = "用户名或密码错误";
        private readonly IConfiguration _configuration;
        private readonly IAdminUserStore _store;
        private readonly PasswordHasher<AdminUser> _passwordHasher;
        private readonly ILogger<AdminAuthService> _logger;

        public AdminAuthService(
            IConfiguration configuration,
            IAdminUserStore store,
            PasswordHasher<AdminUser> passwordHasher,
            ILogger<AdminAuthService> logger)
        {
            _configuration = configuration;
            _store = store;
            _passwordHasher = passwordHasher;
            _logger = logger;
        }

        public async Task<AdminAuthResult> AuthenticateAsync(string username, string password, CancellationToken ct = default)
        {
            username = username.Trim();
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                return AdminAuthResult.Fail(EmptyCredentialsMessage);
            }

            if (IsConfiguredSuperAdmin(username, password))
            {
                return AdminAuthResult.Success(username, AdminIdentityTypes.Super);
            }

            var admin = await _store.GetByUsernameAsync(username, ct);
            if (admin == null || !admin.IsEnabled)
            {
                return AdminAuthResult.Fail(InvalidCredentialsMessage);
            }

            var verification = _passwordHasher.VerifyHashedPassword(admin, admin.PasswordHash, password);
            if (verification == PasswordVerificationResult.Failed)
            {
                return AdminAuthResult.Fail(InvalidCredentialsMessage);
            }

            await _store.UpdateLastLoginTimeAsync(admin.Id, ct);
            return AdminAuthResult.Success(admin.Username, AdminIdentityTypes.Database, admin.Id);
        }

        public async Task<AdminAuthResult> ResetDatabaseAdminPasswordAsync(
            ClaimsPrincipal user,
            string username,
            string newPassword,
            CancellationToken ct = default)
        {
            if (!string.Equals(user.FindFirstValue(AdminClaimTypes.AdminType), AdminIdentityTypes.Super, StringComparison.Ordinal))
            {
                return AdminAuthResult.Fail("无权重置管理员密码");
            }

            username = username.Trim();
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(newPassword))
            {
                return AdminAuthResult.Fail("请输入管理员用户名和新密码");
            }

            var admin = await _store.GetByUsernameAsync(username, ct);
            if (admin == null)
            {
                return AdminAuthResult.Fail("管理员不存在");
            }

            var hash = _passwordHasher.HashPassword(admin, newPassword);
            var changed = await _store.ResetPasswordAsync(username, hash, ct);
            if (!changed)
            {
                _logger.LogWarning("Failed to reset database admin password for {Username}", username);
                return AdminAuthResult.Fail("重置失败");
            }

            return AdminAuthResult.Success(username, AdminIdentityTypes.Database, admin.Id);
        }

        private bool IsConfiguredSuperAdmin(string username, string password)
        {
            var superUsername = _configuration["Admin:Username"];
            var superPassword = _configuration["Admin:Password"];
            if (string.IsNullOrWhiteSpace(superUsername) || string.IsNullOrWhiteSpace(superPassword))
            {
                _logger.LogWarning("Super admin is disabled because Admin:Username or Admin:Password is missing.");
                return false;
            }

            return string.Equals(username, superUsername, StringComparison.Ordinal)
                   && string.Equals(password, superPassword, StringComparison.Ordinal);
        }
    }
}
