using henglong.Web.Controllers;
using henglong.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace henglong.Web.Tests;

public class MiniAdminControllerTests
{
    [Fact]
    public void MiniAdminController_requires_authorization()
    {
        Assert.Contains(typeof(MiniAdminController).GetCustomAttributes(false), item => item is AuthorizeAttribute);
    }

    [Fact]
    public async Task GetCartItems_returns_paginated_cart_items()
    {
        var store = new FakeMiniProgramStore();
        await store.GetOrCreateUserAsync("wx-code-1", null, CancellationToken.None);
        await store.UpsertCartItemAsync("user_1", "guid-1", 2, true, CancellationToken.None);
        var controller = new MiniAdminController(store);

        var result = await controller.GetCartItems(new MiniAdminQueryVm { Current = 1, PageSize = 10 }, CancellationToken.None);

        var json = Assert.IsType<JsonResult>(result);
        var page = Assert.IsType<MiniAdminPageDto<MiniAdminCartItemDto>>(json.Value);
        Assert.Equal(1, page.Total);
        Assert.Equal("M638397-02", page.Data.Single().ProductNumber);
    }

    [Fact]
    public async Task GetAddresses_returns_user_addresses()
    {
        var store = new FakeMiniProgramStore();
        await store.GetOrCreateUserAsync("wx-code-1", null, CancellationToken.None);
        await store.SaveAddressAsync("user_1", new MiniAddressDto { Name = "Alice", Phone = "13800000000", Region = "上海", Detail = "长宁路", IsDefault = true }, CancellationToken.None);
        var controller = new MiniAdminController(store);

        var result = await controller.GetAddresses(new MiniAdminQueryVm { Keyword = "Alice" }, CancellationToken.None);

        var json = Assert.IsType<JsonResult>(result);
        var page = Assert.IsType<MiniAdminPageDto<MiniAdminAddressDto>>(json.Value);
        Assert.Equal(1, page.Total);
        Assert.True(page.Data.Single().IsDefault);
    }
}
