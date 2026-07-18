using henglong.Web.Models;

namespace henglong.Web.Common
{
    public interface IMiniProgramStore
    {
        Task<IReadOnlyList<ImagesVm>> GetEnabledProductsAsync(CancellationToken ct = default);
        Task<ImagesVm?> GetEnabledProductAsync(string guid, CancellationToken ct = default);
        Task<MiniUserDto> GetOrCreateUserAsync(string code, string? openId, CancellationToken ct = default);
        Task<(IReadOnlyList<MiniAdminUserDto> Users, int Total)> GetAdminUsersAsync(string? keyword, int current, int pageSize, CancellationToken ct = default);
        Task<(IReadOnlyList<MiniAdminCartItemDto> Items, int Total)> GetAdminCartItemsAsync(string? keyword, int current, int pageSize, CancellationToken ct = default);
        Task<(IReadOnlyList<MiniAdminFavoriteDto> Items, int Total)> GetAdminFavoritesAsync(string? keyword, int current, int pageSize, CancellationToken ct = default);
        Task<(IReadOnlyList<MiniAdminAddressDto> Items, int Total)> GetAdminAddressesAsync(string? keyword, int current, int pageSize, CancellationToken ct = default);
        Task<IReadOnlyList<string>> GetFavoriteGuidsAsync(string userId, CancellationToken ct = default);
        Task AddFavoriteAsync(string userId, string productGuid, CancellationToken ct = default);
        Task DeleteFavoriteAsync(string userId, string productGuid, CancellationToken ct = default);
        Task<IReadOnlyList<MiniCartItemDto>> GetCartAsync(string userId, CancellationToken ct = default);
        Task UpsertCartItemAsync(string userId, string productGuid, int quantity, bool selected, CancellationToken ct = default);
        Task DeleteCartItemAsync(string userId, string productGuid, CancellationToken ct = default);
        Task<IReadOnlyList<MiniAddressDto>> GetAddressesAsync(string userId, CancellationToken ct = default);
        Task<MiniAddressDto> SaveAddressAsync(string userId, MiniAddressDto address, CancellationToken ct = default);
        Task DeleteAddressAsync(string userId, string addressId, CancellationToken ct = default);
        Task<MiniOrderDto> CreateOrderAsync(string userId, MiniOrderDto order, CancellationToken ct = default);
        Task<IReadOnlyList<MiniOrderDto>> GetOrdersAsync(string userId, string? status, CancellationToken ct = default);
        Task<(IReadOnlyList<MiniOrderDto> Orders, int Total)> GetAdminOrdersAsync(string? status, int current, int pageSize, CancellationToken ct = default);
        Task<MiniOrderDto?> GetOrderAsync(string userId, string orderId, CancellationToken ct = default);
        Task<MiniOrderDto?> MarkOrderPaidAsync(string userId, string orderId, CancellationToken ct = default);
        Task<bool> UpdateOrderStatusAsync(string orderId, string status, CancellationToken ct = default);
    }
}
