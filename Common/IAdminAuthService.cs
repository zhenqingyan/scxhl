using System.Security.Claims;
using henglong.Web.Models;

namespace henglong.Web.Common
{
    public interface IAdminAuthService
    {
        Task<AdminAuthResult> AuthenticateAsync(string username, string password, CancellationToken ct = default);
        Task<AdminAuthResult> ResetDatabaseAdminPasswordAsync(ClaimsPrincipal user, string username, string newPassword, CancellationToken ct = default);
    }
}
