using henglong.Web.Models;

namespace henglong.Web.Common
{
    public interface IAdminUserStore
    {
        void EnsureTableCreated();
        Task<AdminUser?> GetByUsernameAsync(string username, CancellationToken ct = default);
        Task UpdateLastLoginTimeAsync(int id, CancellationToken ct = default);
        Task<bool> ResetPasswordAsync(string username, string passwordHash, CancellationToken ct = default);
    }
}
