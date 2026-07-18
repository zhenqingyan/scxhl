using henglong.Web.Common;
using henglong.Web.Controllers;
using henglong.Web.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace henglong.Web.Tests;

public class MiniProgramControllerTests
{
    [Fact]
    public async Task GetCategories_returns_all_and_composition_categories()
    {
        var controller = CreateController();

        var result = await controller.GetCategories(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var categories = Assert.IsAssignableFrom<IReadOnlyList<MiniCategoryDto>>(ok.Value);
        Assert.Equal("all", categories[0].Id);
        Assert.Contains(categories, category => category.Name == "100%棉");
        Assert.Contains(categories, category => category.Name == "100%人棉");
    }

    [Fact]
    public async Task GetProducts_filters_by_keyword_and_paginates()
    {
        var controller = CreateController();

        var result = await controller.GetProducts("638397", "all", 1, 1, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var page = Assert.IsType<MiniPagedResult<MiniProductDto>>(ok.Value);
        Assert.Equal(1, page.Page);
        Assert.Equal(1, page.PageSize);
        Assert.Equal(1, page.Total);
        Assert.Equal("M638397-02", page.Items.Single().Number);
    }

    [Fact]
    public async Task GetProduct_returns_not_found_for_missing_guid()
    {
        var controller = CreateController();

        var result = await controller.GetProduct("missing-guid", CancellationToken.None);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task Login_returns_stable_user_for_code()
    {
        var controller = CreateController();

        var first = await controller.Login(new MiniLoginRequest { Code = "wx-code-1" }, CancellationToken.None);
        var second = await controller.Login(new MiniLoginRequest { Code = "wx-code-1" }, CancellationToken.None);

        var firstOk = Assert.IsType<OkObjectResult>(first.Result);
        var secondOk = Assert.IsType<OkObjectResult>(second.Result);
        var firstLogin = Assert.IsType<MiniLoginResponse>(firstOk.Value);
        var secondLogin = Assert.IsType<MiniLoginResponse>(secondOk.Value);
        Assert.Equal(firstLogin.UserId, secondLogin.UserId);
        Assert.False(string.IsNullOrWhiteSpace(firstLogin.Token));
    }

    [Fact]
    public async Task Favorites_can_toggle_product_for_current_user()
    {
        var controller = CreateController("user_1");

        var add = await controller.AddFavorite(new MiniFavoriteRequest { ProductGuid = "guid-1" }, CancellationToken.None);
        var list = await controller.GetFavorites(CancellationToken.None);
        var remove = await controller.DeleteFavorite("guid-1", CancellationToken.None);
        var empty = await controller.GetFavorites(CancellationToken.None);

        Assert.IsType<OkObjectResult>(add);
        Assert.IsType<OkObjectResult>(remove);
        var listOk = Assert.IsType<OkObjectResult>(list.Result);
        var favorites = Assert.IsAssignableFrom<IReadOnlyList<MiniProductDto>>(listOk.Value);
        Assert.Single(favorites);
        var emptyOk = Assert.IsType<OkObjectResult>(empty.Result);
        Assert.Empty(Assert.IsAssignableFrom<IReadOnlyList<MiniProductDto>>(emptyOk.Value));
    }

    [Fact]
    public async Task Cart_can_add_update_and_delete_items()
    {
        var controller = CreateController("user_1");

        await controller.AddCartItem(new MiniCartUpsertRequest { ProductGuid = "guid-1", Quantity = 2, Selected = true }, CancellationToken.None);
        await controller.UpdateCartItem("guid-1", new MiniCartUpdateRequest { Quantity = 4, Selected = false }, CancellationToken.None);
        var list = await controller.GetCart(CancellationToken.None);
        await controller.DeleteCartItem("guid-1", CancellationToken.None);
        var empty = await controller.GetCart(CancellationToken.None);

        var listOk = Assert.IsType<OkObjectResult>(list.Result);
        var cart = Assert.IsType<MiniCartDto>(listOk.Value);
        Assert.Single(cart.Items);
        Assert.Equal(4, cart.Items[0].Quantity);
        Assert.False(cart.Items[0].Selected);
        var emptyOk = Assert.IsType<OkObjectResult>(empty.Result);
        Assert.Empty(Assert.IsType<MiniCartDto>(emptyOk.Value).Items);
    }

    [Fact]
    public async Task Addresses_keep_one_default_address()
    {
        var controller = CreateController("user_1");

        await controller.SaveAddress(new MiniAddressRequest { Name = "Alice", Phone = "13800000000", Region = "Tianjin", Detail = "No. 1", IsDefault = true }, CancellationToken.None);
        await controller.SaveAddress(new MiniAddressRequest { Name = "Bob", Phone = "13900000000", Region = "Shanghai", Detail = "No. 2", IsDefault = true }, CancellationToken.None);
        var result = await controller.GetAddresses(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var addresses = Assert.IsAssignableFrom<IReadOnlyList<MiniAddressDto>>(ok.Value);
        Assert.Equal(2, addresses.Count);
        Assert.Single(addresses, address => address.IsDefault);
        Assert.Equal("Bob", addresses.Single(address => address.IsDefault).Name);
    }

    [Fact]
    public async Task Orders_can_be_created_and_paid()
    {
        var controller = CreateController("user_1");
        var addressResult = await controller.SaveAddress(new MiniAddressRequest { Name = "Alice", Phone = "13800000000", Region = "Tianjin", Detail = "No. 1", IsDefault = true }, CancellationToken.None);
        var address = Assert.IsType<MiniAddressDto>(Assert.IsType<OkObjectResult>(addressResult.Result).Value);

        var created = await controller.CreateOrder(new MiniCreateOrderRequest
        {
            AddressId = address.Id,
            Items = [new MiniOrderItemRequest { ProductGuid = "guid-1", Quantity = 2 }]
        }, CancellationToken.None);
        var createdOrder = Assert.IsType<MiniOrderDto>(Assert.IsType<OkObjectResult>(created.Result).Value);

        Assert.Equal("pendingPay", createdOrder.Status);
        Assert.Equal(60m, createdOrder.Total);
        var paid = await controller.PayOrder(createdOrder.Id, CancellationToken.None);
        var paidOrder = Assert.IsType<MiniOrderDto>(Assert.IsType<OkObjectResult>(paid.Result).Value);
        Assert.Equal("pendingShip", paidOrder.Status);
    }

    private static MiniProgramController CreateController(string? userId = null)
    {
        var controller = new MiniProgramController(new FakeMiniProgramStore());
        var httpContext = new DefaultHttpContext();
        if (!string.IsNullOrWhiteSpace(userId))
        {
            httpContext.Request.Headers["X-Mini-User-Id"] = userId;
        }
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        return controller;
    }
}

public class OrderControllerTests
{
    [Fact]
    public async Task GetOrders_returns_paginated_orders_for_admin_page()
    {
        var store = new FakeMiniProgramStore();
        var miniController = CreateMiniController(store, "user_1");
        var addressResult = await miniController.SaveAddress(new MiniAddressRequest { Name = "Alice", Phone = "13800000000", Region = "Tianjin", Detail = "No. 1", IsDefault = true }, CancellationToken.None);
        var address = Assert.IsType<MiniAddressDto>(Assert.IsType<OkObjectResult>(addressResult.Result).Value);
        await miniController.CreateOrder(new MiniCreateOrderRequest
        {
            AddressId = address.Id,
            Items = [new MiniOrderItemRequest { ProductGuid = "guid-1", Quantity = 1 }]
        }, CancellationToken.None);
        var controller = new OrderController(store);

        var result = await controller.GetOrders(new OrderQueryVm { Current = 1, PageSize = 10, Status = "all" }, CancellationToken.None);

        var ok = Assert.IsType<JsonResult>(result);
        var page = Assert.IsType<AdminOrderPageDto>(ok.Value);
        Assert.Equal(1, page.Total);
        Assert.Equal("pendingPay", page.Data.Single().Status);
        Assert.Equal("Alice", page.Data.Single().Address?.Name);
    }

    [Fact]
    public async Task UpdateStatus_changes_order_status()
    {
        var store = new FakeMiniProgramStore();
        var miniController = CreateMiniController(store, "user_1");
        var addressResult = await miniController.SaveAddress(new MiniAddressRequest { Name = "Alice", Phone = "13800000000", Region = "Tianjin", Detail = "No. 1", IsDefault = true }, CancellationToken.None);
        var address = Assert.IsType<MiniAddressDto>(Assert.IsType<OkObjectResult>(addressResult.Result).Value);
        var created = await miniController.CreateOrder(new MiniCreateOrderRequest
        {
            AddressId = address.Id,
            Items = [new MiniOrderItemRequest { ProductGuid = "guid-1", Quantity = 1 }]
        }, CancellationToken.None);
        var order = Assert.IsType<MiniOrderDto>(Assert.IsType<OkObjectResult>(created.Result).Value);
        var controller = new OrderController(store);

        var result = await controller.UpdateStatus(new OrderStatusUpdateVm { OrderId = order.Id, Status = "shipped" }, CancellationToken.None);

        var json = Assert.IsType<JsonResult>(result);
        Assert.Equal("成功", json.Value);
        var updated = await store.GetOrderAsync("user_1", order.Id, CancellationToken.None);
        Assert.Equal("shipped", updated?.Status);
    }

    private static MiniProgramController CreateMiniController(FakeMiniProgramStore store, string userId)
    {
        var controller = new MiniProgramController(store);
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-Mini-User-Id"] = userId;
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        return controller;
    }
}

internal sealed class FakeMiniProgramStore : IMiniProgramStore
{
    private readonly List<ImagesVm> _products =
    [
        new ImagesVm { Id = 1, Guid = "guid-1", Status = true, Name = "M638397-02.jpg", Level = 3, Number = "M638397-02", Composition = "100%棉", YarnCount = "32*32竹节", Density = "78*22", GramWeight = "110.6 GSM", Doorframe = "142.2CM/56英寸", Width = 100, Height = 120, Percent = 1.2m },
        new ImagesVm { Id = 2, Guid = "guid-2", Status = true, Name = "M514156-04.jpg", Level = 2, Number = "M514156-04", Composition = "100%人棉", YarnCount = "32竹节", Density = "90*72", GramWeight = "143.2 GSM", Doorframe = "142.2CM/56英寸", Width = 100, Height = 110, Percent = 1.1m },
        new ImagesVm { Id = 3, Guid = "guid-hidden", Status = false, Name = "hidden.jpg", Level = 1, Number = "HIDDEN", Composition = "100%棉" }
    ];
    private readonly Dictionary<string, MiniUserDto> _usersByCode = [];
    private readonly Dictionary<string, HashSet<string>> _favorites = [];
    private readonly Dictionary<string, List<MiniCartItemDto>> _cart = [];
    private readonly Dictionary<string, List<MiniAddressDto>> _addresses = [];
    private readonly Dictionary<string, List<MiniOrderDto>> _orders = [];

    public Task<IReadOnlyList<ImagesVm>> GetEnabledProductsAsync(CancellationToken ct = default)
    {
        return Task.FromResult<IReadOnlyList<ImagesVm>>(_products.Where(product => product.Status).ToList());
    }

    public Task<ImagesVm?> GetEnabledProductAsync(string guid, CancellationToken ct = default)
    {
        return Task.FromResult(_products.FirstOrDefault(product => product.Status && product.Guid == guid));
    }

    public Task<MiniUserDto> GetOrCreateUserAsync(string code, string? openId, CancellationToken ct = default)
    {
        var key = !string.IsNullOrWhiteSpace(openId) ? openId : code;
        if (!_usersByCode.TryGetValue(key, out var user))
        {
            user = new MiniUserDto("user_" + (_usersByCode.Count + 1), openId ?? "mock_" + code);
            _usersByCode[key] = user;
        }
        return Task.FromResult(user);
    }

    public Task<(IReadOnlyList<MiniAdminUserDto> Users, int Total)> GetAdminUsersAsync(string? keyword, int current, int pageSize, CancellationToken ct = default)
    {
        var users = _usersByCode.Values.Select(user => new MiniAdminUserDto
        {
            UserId = user.UserId,
            OpenId = user.OpenId,
            CreateTime = DateTime.Now,
            CartCount = _cart.GetValueOrDefault(user.UserId)?.Count ?? 0,
            FavoriteCount = _favorites.GetValueOrDefault(user.UserId)?.Count ?? 0,
            AddressCount = _addresses.GetValueOrDefault(user.UserId)?.Count ?? 0,
            OrderCount = _orders.GetValueOrDefault(user.UserId)?.Count ?? 0
        }).ToList();
        users = Filter(users, keyword, user => user.UserId + " " + user.OpenId);
        return Task.FromResult(PageResult<MiniAdminUserDto>(users, current, pageSize));
    }

    public Task<(IReadOnlyList<MiniAdminCartItemDto> Items, int Total)> GetAdminCartItemsAsync(string? keyword, int current, int pageSize, CancellationToken ct = default)
    {
        var items = _cart.SelectMany(pair => pair.Value.Select(item =>
        {
            var product = _products.FirstOrDefault(product => product.Guid == item.ProductGuid);
            return new MiniAdminCartItemDto
            {
                UserId = pair.Key,
                ProductGuid = item.ProductGuid,
                Quantity = item.Quantity,
                Selected = item.Selected,
                UpdateTime = DateTime.Now,
                ProductNumber = product?.Number ?? string.Empty,
                ProductName = product?.Name ?? string.Empty,
                Composition = product?.Composition ?? string.Empty
            };
        })).ToList();
        items = Filter(items, keyword, item => item.UserId + " " + item.ProductGuid + " " + item.ProductNumber + " " + item.ProductName);
        return Task.FromResult(PageResult<MiniAdminCartItemDto>(items, current, pageSize));
    }

    public Task<(IReadOnlyList<MiniAdminFavoriteDto> Items, int Total)> GetAdminFavoritesAsync(string? keyword, int current, int pageSize, CancellationToken ct = default)
    {
        var items = _favorites.SelectMany(pair => pair.Value.Select(productGuid =>
        {
            var product = _products.FirstOrDefault(product => product.Guid == productGuid);
            return new MiniAdminFavoriteDto
            {
                UserId = pair.Key,
                ProductGuid = productGuid,
                CreateTime = DateTime.Now,
                ProductNumber = product?.Number ?? string.Empty,
                ProductName = product?.Name ?? string.Empty,
                Composition = product?.Composition ?? string.Empty
            };
        })).ToList();
        items = Filter(items, keyword, item => item.UserId + " " + item.ProductGuid + " " + item.ProductNumber + " " + item.ProductName);
        return Task.FromResult(PageResult<MiniAdminFavoriteDto>(items, current, pageSize));
    }

    public Task<(IReadOnlyList<MiniAdminAddressDto> Items, int Total)> GetAdminAddressesAsync(string? keyword, int current, int pageSize, CancellationToken ct = default)
    {
        var items = _addresses.SelectMany(pair => pair.Value.Select(address => new MiniAdminAddressDto
        {
            Id = address.Id,
            UserId = pair.Key,
            Name = address.Name,
            Phone = address.Phone,
            Region = address.Region,
            Detail = address.Detail,
            IsDefault = address.IsDefault
        })).ToList();
        items = Filter(items, keyword, item => item.UserId + " " + item.Name + " " + item.Phone + " " + item.Region + " " + item.Detail);
        return Task.FromResult(PageResult<MiniAdminAddressDto>(items, current, pageSize));
    }

    public Task<IReadOnlyList<string>> GetFavoriteGuidsAsync(string userId, CancellationToken ct = default)
    {
        return Task.FromResult<IReadOnlyList<string>>(_favorites.GetValueOrDefault(userId)?.ToList() ?? []);
    }

    public Task AddFavoriteAsync(string userId, string productGuid, CancellationToken ct = default)
    {
        if (!_favorites.ContainsKey(userId)) _favorites[userId] = [];
        _favorites[userId].Add(productGuid);
        return Task.CompletedTask;
    }

    public Task DeleteFavoriteAsync(string userId, string productGuid, CancellationToken ct = default)
    {
        _favorites.GetValueOrDefault(userId)?.Remove(productGuid);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<MiniCartItemDto>> GetCartAsync(string userId, CancellationToken ct = default)
    {
        return Task.FromResult<IReadOnlyList<MiniCartItemDto>>(_cart.GetValueOrDefault(userId)?.ToList() ?? []);
    }

    public Task UpsertCartItemAsync(string userId, string productGuid, int quantity, bool selected, CancellationToken ct = default)
    {
        if (!_cart.ContainsKey(userId)) _cart[userId] = [];
        var item = _cart[userId].FirstOrDefault(cartItem => cartItem.ProductGuid == productGuid);
        if (item == null)
        {
            _cart[userId].Add(new MiniCartItemDto(productGuid, quantity, selected));
        }
        else
        {
            item.Quantity = quantity;
            item.Selected = selected;
        }
        return Task.CompletedTask;
    }

    public Task DeleteCartItemAsync(string userId, string productGuid, CancellationToken ct = default)
    {
        _cart.GetValueOrDefault(userId)?.RemoveAll(cartItem => cartItem.ProductGuid == productGuid);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<MiniAddressDto>> GetAddressesAsync(string userId, CancellationToken ct = default)
    {
        return Task.FromResult<IReadOnlyList<MiniAddressDto>>(_addresses.GetValueOrDefault(userId)?.ToList() ?? []);
    }

    public Task<MiniAddressDto> SaveAddressAsync(string userId, MiniAddressDto address, CancellationToken ct = default)
    {
        if (!_addresses.ContainsKey(userId)) _addresses[userId] = [];
        if (string.IsNullOrWhiteSpace(address.Id)) address.Id = "addr_" + (_addresses[userId].Count + 1);
        if (address.IsDefault)
        {
            foreach (var item in _addresses[userId]) item.IsDefault = false;
        }
        _addresses[userId].RemoveAll(item => item.Id == address.Id);
        _addresses[userId].Add(address);
        if (!_addresses[userId].Any(item => item.IsDefault)) _addresses[userId][0].IsDefault = true;
        return Task.FromResult(address);
    }

    public Task DeleteAddressAsync(string userId, string addressId, CancellationToken ct = default)
    {
        _addresses.GetValueOrDefault(userId)?.RemoveAll(address => address.Id == addressId);
        return Task.CompletedTask;
    }

    public Task<MiniOrderDto> CreateOrderAsync(string userId, MiniOrderDto order, CancellationToken ct = default)
    {
        if (!_orders.ContainsKey(userId)) _orders[userId] = [];
        order.Id = "order_" + (_orders[userId].Count + 1);
        order.Status = "pendingPay";
        _orders[userId].Add(order);
        return Task.FromResult(order);
    }

    public Task<IReadOnlyList<MiniOrderDto>> GetOrdersAsync(string userId, string? status, CancellationToken ct = default)
    {
        var orders = _orders.GetValueOrDefault(userId) ?? [];
        if (!string.IsNullOrWhiteSpace(status) && status != "all")
        {
            orders = orders.Where(order => order.Status == status).ToList();
        }
        return Task.FromResult<IReadOnlyList<MiniOrderDto>>(orders.ToList());
    }

    public Task<(IReadOnlyList<MiniOrderDto> Orders, int Total)> GetAdminOrdersAsync(string? status, int current, int pageSize, CancellationToken ct = default)
    {
        var orders = _orders.Values.SelectMany(items => items).ToList();
        if (!string.IsNullOrWhiteSpace(status) && status != "all")
        {
            orders = orders.Where(order => order.Status == status).ToList();
        }

        var total = orders.Count;
        var page = orders.Skip((Math.Max(1, current) - 1) * Math.Max(1, pageSize)).Take(Math.Max(1, pageSize)).ToList();
        return Task.FromResult<(IReadOnlyList<MiniOrderDto> Orders, int Total)>((page, total));
    }

    public Task<MiniOrderDto?> GetOrderAsync(string userId, string orderId, CancellationToken ct = default)
    {
        return Task.FromResult(_orders.GetValueOrDefault(userId)?.FirstOrDefault(order => order.Id == orderId));
    }

    public Task<MiniOrderDto?> MarkOrderPaidAsync(string userId, string orderId, CancellationToken ct = default)
    {
        var order = _orders.GetValueOrDefault(userId)?.FirstOrDefault(item => item.Id == orderId);
        if (order != null) order.Status = "pendingShip";
        return Task.FromResult(order);
    }

    public Task<bool> UpdateOrderStatusAsync(string orderId, string status, CancellationToken ct = default)
    {
        var order = _orders.Values.SelectMany(items => items).FirstOrDefault(item => item.Id == orderId);
        if (order == null) return Task.FromResult(false);
        order.Status = status;
        return Task.FromResult(true);
    }

    private static List<T> Filter<T>(List<T> items, string? keyword, Func<T, string> selector)
    {
        if (string.IsNullOrWhiteSpace(keyword)) return items;
        return items.Where(item => selector(item).Contains(keyword, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    private static (IReadOnlyList<T> Items, int Total) PageResult<T>(List<T> items, int current, int pageSize)
    {
        current = Math.Max(1, current);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var total = items.Count;
        return (items.Skip((current - 1) * pageSize).Take(pageSize).ToList(), total);
    }
}
