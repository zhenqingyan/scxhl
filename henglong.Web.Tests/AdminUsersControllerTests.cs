using System.Security.Claims;
using henglong.Web.Common;
using henglong.Web.Controllers;
using henglong.Web.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace henglong.Web.Tests;

public class AdminUsersControllerTests
{
    [Fact]
    public void AdminUsersController_requires_authorization()
    {
        Assert.Contains(typeof(AdminUsersController).GetCustomAttributes(false), item => item is Microsoft.AspNetCore.Authorization.AuthorizeAttribute);
    }

    [Fact]
    public async Task Create_rejects_database_admin()
    {
        var store = new FakeAdminUserStore();
        var controller = CreateController(store, AdminIdentityTypes.Database);

        var result = await controller.Create(new AdminUserCreateVm { Username = "alice", Password = "secret" }, CancellationToken.None);

        Assert.IsType<ForbidResult>(result);
        Assert.Empty(store.Users);
    }

    [Fact]
    public async Task Create_adds_database_admin_for_super_admin()
    {
        var store = new FakeAdminUserStore();
        var controller = CreateController(store, AdminIdentityTypes.Super);

        var result = await controller.Create(new AdminUserCreateVm { Username = "alice", Password = "secret" }, CancellationToken.None);

        var json = Assert.IsType<JsonResult>(result);
        Assert.True((bool)json.Value!.GetType().GetProperty("success")!.GetValue(json.Value)!);
        Assert.True(store.Users["alice"].IsEnabled);
        Assert.NotEqual("secret", store.Users["alice"].PasswordHash);
    }

    [Fact]
    public async Task SetEnabled_updates_database_admin_status_for_super_admin()
    {
        var store = new FakeAdminUserStore();
        store.Users["alice"] = new AdminUser { Id = 1, Username = "alice", IsEnabled = true };
        var controller = CreateController(store, AdminIdentityTypes.Super);

        var result = await controller.SetEnabled(new AdminUserStatusVm { Username = "alice", IsEnabled = false }, CancellationToken.None);

        var json = Assert.IsType<JsonResult>(result);
        Assert.True((bool)json.Value!.GetType().GetProperty("success")!.GetValue(json.Value)!);
        Assert.False(store.Users["alice"].IsEnabled);
    }

    [Fact]
    public void ResetPassword_requires_an_antiforgery_token()
    {
        var action = typeof(AdminUsersController).GetMethod(nameof(AdminUsersController.ResetPassword));

        Assert.NotNull(action);
        Assert.Contains(action.GetCustomAttributes(inherit: true), attribute => attribute is ValidateAntiForgeryTokenAttribute);
    }

    private static AdminUsersController CreateController(FakeAdminUserStore store, string adminType)
    {
        var controller = new AdminUsersController(store, new PasswordHasher<AdminUser>());
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity([
                    new Claim(ClaimTypes.Name, "root"),
                    new Claim(AdminClaimTypes.AdminType, adminType)
                ], "Test"))
            }
        };
        return controller;
    }

    private sealed class FakeAdminUserStore : IAdminUserStore
    {
        public Dictionary<string, AdminUser> Users { get; } = new(StringComparer.OrdinalIgnoreCase);

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
            admin.Id = Users.Count + 1;
            Users[admin.Username] = admin;
            return Task.FromResult(true);
        }

        public Task UpdateLastLoginTimeAsync(int id, CancellationToken ct = default)
        {
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
            user.UpdateTime = DateTime.Now;
            return Task.FromResult(true);
        }
    }
}
