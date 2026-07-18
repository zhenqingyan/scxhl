using henglong.Web.Models;

namespace henglong.Web.Common
{
    public interface IAdminUserStore
    {
        void EnsureTableCreated();
        Task<IReadOnlyList<AdminUser>> GetAllAsync(CancellationToken ct = default);
        Task<AdminUser?> GetByUsernameAsync(string username, CancellationToken ct = default);
        Task<bool> CreateAsync(AdminUser admin, CancellationToken ct = default);
        Task UpdateLastLoginTimeAsync(int id, CancellationToken ct = default);
        Task<bool> ResetPasswordAsync(string username, string passwordHash, CancellationToken ct = default);
        Task<bool> SetEnabledAsync(string username, bool isEnabled, CancellationToken ct = default);
    }
}
