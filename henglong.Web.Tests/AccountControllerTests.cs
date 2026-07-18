using System.Security.Claims;
using henglong.Web.Common;
using henglong.Web.Controllers;
using henglong.Web.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace henglong.Web.Tests;

public class AccountControllerTests
{
    [Fact]
    public void ResetAdminPassword_requires_an_antiforgery_token()
    {
        var action = typeof(AccountController).GetMethod(nameof(AccountController.ResetAdminPassword));

        Assert.NotNull(action);
        Assert.Contains(action.GetCustomAttributes(inherit: true), attribute => attribute is ValidateAntiForgeryTokenAttribute);
    }

    [Fact]
    public async Task ResetAdminPassword_rejects_empty_new_password()
    {
        var controller = CreateController(new FakeAdminAuthService());

        var result = await controller.ResetAdminPassword(new AdminResetPasswordVm { Username = "alice", NewPassword = "" }, CancellationToken.None);

        var json = Assert.IsType<JsonResult>(result);
        Assert.False((bool)json.Value!.GetType().GetProperty("success")!.GetValue(json.Value)!);
        Assert.Equal("请输入管理员用户名和新密码", json.Value.GetType().GetProperty("message")!.GetValue(json.Value));
    }

    [Fact]
    public async Task ResetAdminPassword_returns_success_when_service_allows_it()
    {
        var service = new FakeAdminAuthService { ResetResult = AdminAuthResult.Success("alice", AdminIdentityTypes.Database, 1) };
        var controller = CreateController(service);

        var result = await controller.ResetAdminPassword(new AdminResetPasswordVm { Username = "alice", NewPassword = "new-secret" }, CancellationToken.None);

        var json = Assert.IsType<JsonResult>(result);
        Assert.True((bool)json.Value!.GetType().GetProperty("success")!.GetValue(json.Value)!);
        Assert.Equal("成功", json.Value.GetType().GetProperty("message")!.GetValue(json.Value));
    }

    private static AccountController CreateController(FakeAdminAuthService service)
    {
        var controller = new AccountController(service);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity([
                    new Claim(ClaimTypes.Name, "root"),
                    new Claim(AdminClaimTypes.AdminType, AdminIdentityTypes.Super)
                ], "Test"))
            }
        };
        return controller;
    }

    private sealed class FakeAdminAuthService : IAdminAuthService
    {
        public AdminAuthResult ResetResult { get; set; } = AdminAuthResult.Fail("请输入管理员用户名和新密码");

        public Task<AdminAuthResult> AuthenticateAsync(string username, string password, CancellationToken ct = default)
        {
            return Task.FromResult(AdminAuthResult.Fail("用户名或密码错误"));
        }

        public Task<AdminAuthResult> ResetDatabaseAdminPasswordAsync(ClaimsPrincipal user, string username, string newPassword, CancellationToken ct = default)
        {
            return Task.FromResult(ResetResult);
        }
    }
}
