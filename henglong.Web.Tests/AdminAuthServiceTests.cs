using System.Security.Claims;
using henglong.Web.Common;
using henglong.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace henglong.Web.Tests;

public class AdminAuthServiceTests
{
    [Fact]
    public async Task AuthenticateAsync_accepts_configured_super_admin()
    {
        var store = new FakeAdminUserStore();
        var service = CreateService(store, new Dictionary<string, string?>
        {
            ["Admin:Username"] = "root",
            ["Admin:Password"] = "secret"
        });

        var result = await service.AuthenticateAsync("root", "secret", CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(AdminIdentityTypes.Super, result.AdminType);
        Assert.Equal("root", result.Username);
        Assert.Null(result.DatabaseAdminId);
    }

    [Fact]
    public async Task AuthenticateAsync_rejects_super_admin_when_config_is_missing()
    {
        var store = new FakeAdminUserStore();
        var service = CreateService(store, new Dictionary<string, string?>
        {
            ["Admin:Username"] = "root"
        });

        var result = await service.AuthenticateAsync("root", "secret", CancellationToken.None);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task AuthenticateAsync_accepts_database_admin_with_correct_password()
    {
        var store = new FakeAdminUserStore();
        var hasher = new PasswordHasher<AdminUser>();
        store.Users["alice"] = new AdminUser
        {
            Id = 7,
            Username = "alice",
            IsEnabled = true,
            PasswordHash = hasher.HashPassword(new AdminUser { Username = "alice" }, "db-secret")
        };
        var service = CreateService(store);

        var result = await service.AuthenticateAsync("alice", "db-secret", CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Equal(AdminIdentityTypes.Database, result.AdminType);
        Assert.Equal(7, result.DatabaseAdminId);
        Assert.Equal(7, store.LastLoginAdminId);
    }

    [Fact]
    public async Task AuthenticateAsync_rejects_database_admin_with_wrong_password()
    {
        var store = new FakeAdminUserStore();
        var hasher = new PasswordHasher<AdminUser>();
        store.Users["alice"] = new AdminUser
        {
            Id = 7,
            Username = "alice",
            IsEnabled = true,
            PasswordHash = hasher.HashPassword(new AdminUser { Username = "alice" }, "db-secret")
        };
        var service = CreateService(store);

        var result = await service.AuthenticateAsync("alice", "wrong", CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Null(store.LastLoginAdminId);
    }

    [Fact]
    public async Task AuthenticateAsync_rejects_disabled_database_admin()
    {
        var store = new FakeAdminUserStore();
        var hasher = new PasswordHasher<AdminUser>();
        store.Users["alice"] = new AdminUser
        {
            Id = 7,
            Username = "alice",
            IsEnabled = false,
            PasswordHash = hasher.HashPassword(new AdminUser { Username = "alice" }, "db-secret")
        };
        var service = CreateService(store);

        var result = await service.AuthenticateAsync("alice", "db-secret", CancellationToken.None);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task ResetDatabaseAdminPasswordAsync_allows_super_admin()
    {
        var store = new FakeAdminUserStore();
        store.Users["alice"] = new AdminUser { Id = 7, Username = "alice", IsEnabled = true, PasswordHash = "old" };
        var service = CreateService(store);
        var principal = CreatePrincipal(AdminIdentityTypes.Super, "root");

        var result = await service.ResetDatabaseAdminPasswordAsync(principal, "alice", "new-secret", CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.NotEqual("old", store.Users["alice"].PasswordHash);
        Assert.NotEqual(default, store.Users["alice"].UpdateTime);
    }

    [Fact]
    public async Task ResetDatabaseAdminPasswordAsync_rejects_database_admin()
    {
        var store = new FakeAdminUserStore();
        store.Users["alice"] = new AdminUser { Id = 7, Username = "alice", IsEnabled = true, PasswordHash = "old" };
        var service = CreateService(store);
        var principal = CreatePrincipal(AdminIdentityTypes.Database, "bob");

        var result = await service.ResetDatabaseAdminPasswordAsync(principal, "alice", "new-secret", CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal("无权重置管理员密码", result.Message);
        Assert.Equal("old", store.Users["alice"].PasswordHash);
    }

    [Fact]
    public async Task ResetDatabaseAdminPasswordAsync_rejects_missing_target_admin()
    {
        var store = new FakeAdminUserStore();
        var service = CreateService(store);
        var principal = CreatePrincipal(AdminIdentityTypes.Super, "root");

        var result = await service.ResetDatabaseAdminPasswordAsync(principal, "missing", "new-secret", CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Equal("管理员不存在", result.Message);
    }

    private static AdminAuthService CreateService(
        FakeAdminUserStore store,
        Dictionary<string, string?>? values = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values ?? new Dictionary<string, string?>())
            .Build();
        return new AdminAuthService(
            configuration,
            store,
            new PasswordHasher<AdminUser>(),
            NullLogger<AdminAuthService>.Instance);
    }

    private static ClaimsPrincipal CreatePrincipal(string adminType, string name)
    {
        var identity = new ClaimsIdentity([
            new Claim(ClaimTypes.Name, name),
            new Claim(AdminClaimTypes.AdminType, adminType)
        ], "Test");
        return new ClaimsPrincipal(identity);
    }

    private sealed class FakeAdminUserStore : IAdminUserStore
    {
        public Dictionary<string, AdminUser> Users { get; } = new(StringComparer.OrdinalIgnoreCase);
        public int? LastLoginAdminId { get; private set; }

        public void EnsureTableCreated() { }

        public Task<IReadOnlyList<AdminUser>> GetAllAsync(CancellationToken ct = default)
        {
            return Task.FromResult<IReadOnlyList<AdminUser>>(Users.Values.ToList());
        }

        public Task<AdminUser?> GetByUsernameAsync(string username, CancellationToken ct = default)
        {
            Users.TryGetValue(username, out var user);
            return Task.FromResult(user);
        }

        public Task<bool> CreateAsync(AdminUser admin, CancellationToken ct = default)
        {
            if (Users.ContainsKey(admin.Username)) return Task.FromResult(false);
            Users[admin.Username] = admin;
            return Task.FromResult(true);
        }

        public Task UpdateLastLoginTimeAsync(int id, CancellationToken ct = default)
        {
            LastLoginAdminId = id;
            return Task.CompletedTask;
        }

        public Task<bool> ResetPasswordAsync(string username, string passwordHash, CancellationToken ct = default)
        {
            if (!Users.TryGetValue(username, out var user)) return Task.FromResult(false);
            user.PasswordHash = passwordHash;
            user.UpdateTime = DateTime.Now;
            return Task.FromResult(true);
        }

        public Task<bool> SetEnabledAsync(string username, bool isEnabled, CancellationToken ct = default)
        {
            if (!Users.TryGetValue(username, out var user)) return Task.FromResult(false);
            user.IsEnabled = isEnabled;
            return Task.FromResult(true);
        }
    }
}

public class AdminUserStoreShapeTests
{
    [Fact]
    public void AdminUserStore_implements_IAdminUserStore()
    {
        Assert.True(typeof(IAdminUserStore).IsAssignableFrom(typeof(AdminUserStore)));
    }
}
