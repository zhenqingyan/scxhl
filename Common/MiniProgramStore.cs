using System.Text.Json;
using Dapper;
using henglong.Web.Models;
using MySqlConnector;

namespace henglong.Web.Common
{
    public class MiniProgramStore : IMiniProgramStore
    {
        private readonly string _connectionString;
        private readonly ILogger<MiniProgramStore> _logger;
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        public MiniProgramStore(IConfiguration configuration, ILogger<MiniProgramStore> logger)
        {
            _connectionString = configuration.GetConnectionString("MySql") ?? string.Empty;
            _logger = logger;
        }

        public void EnsureTablesCreated()
        {
            var sql = @"
                CREATE TABLE IF NOT EXISTS `mini_users` (
                  `UserId`     VARCHAR(64)  NOT NULL,
                  `OpenId`     VARCHAR(128) NOT NULL,
                  `Code`       VARCHAR(128) NOT NULL DEFAULT '',
                  `CreateTime` DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
                  PRIMARY KEY (`UserId`),
                  UNIQUE KEY `uk_openid` (`OpenId`)
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

                CREATE TABLE IF NOT EXISTS `mini_favorites` (
                  `UserId`      VARCHAR(64) NOT NULL,
                  `ProductGuid` VARCHAR(64) NOT NULL,
                  `CreateTime`  DATETIME    NOT NULL DEFAULT CURRENT_TIMESTAMP,
                  PRIMARY KEY (`UserId`, `ProductGuid`)
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

                CREATE TABLE IF NOT EXISTS `mini_cart_items` (
                  `UserId`      VARCHAR(64) NOT NULL,
                  `ProductGuid` VARCHAR(64) NOT NULL,
                  `Quantity`    INT         NOT NULL DEFAULT 1,
                  `Selected`    TINYINT(1)  NOT NULL DEFAULT 1,
                  `UpdateTime`  DATETIME    NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
                  PRIMARY KEY (`UserId`, `ProductGuid`)
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

                CREATE TABLE IF NOT EXISTS `mini_addresses` (
                  `Id`       VARCHAR(64)  NOT NULL,
                  `UserId`   VARCHAR(64)  NOT NULL,
                  `Name`     VARCHAR(100) NOT NULL DEFAULT '',
                  `Phone`    VARCHAR(32)  NOT NULL DEFAULT '',
                  `Region`   VARCHAR(255) NOT NULL DEFAULT '',
                  `Detail`   VARCHAR(255) NOT NULL DEFAULT '',
                  `IsDefault` TINYINT(1)  NOT NULL DEFAULT 0,
                  PRIMARY KEY (`Id`),
                  KEY `idx_user` (`UserId`)
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

                CREATE TABLE IF NOT EXISTS `mini_orders` (
                  `Id`             VARCHAR(64)    NOT NULL,
                  `UserId`         VARCHAR(64)    NOT NULL,
                  `Status`         VARCHAR(32)    NOT NULL DEFAULT 'pendingPay',
                  `AddressJson`    JSON           NOT NULL,
                  `ItemsJson`      JSON           NOT NULL,
                  `Subtotal`       DECIMAL(10,2)  NOT NULL DEFAULT 0.00,
                  `Freight`        DECIMAL(10,2)  NOT NULL DEFAULT 0.00,
                  `Total`          DECIMAL(10,2)  NOT NULL DEFAULT 0.00,
                  `DeliveryMethod` VARCHAR(32)    NOT NULL DEFAULT 'sf',
                  `InvoiceType`    VARCHAR(32)    NOT NULL DEFAULT 'none',
                  `CreateTime`     DATETIME       NOT NULL DEFAULT CURRENT_TIMESTAMP,
                  `PayTime`        DATETIME       NULL,
                  PRIMARY KEY (`Id`),
                  KEY `idx_user_status` (`UserId`, `Status`)
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;";

            using var conn = new MySqlConnection(_connectionString);
            conn.Execute(sql);
        }

        public async Task<IReadOnlyList<ImagesVm>> GetEnabledProductsAsync(CancellationToken ct = default)
        {
            var sql = @"SELECT Id, Guid, Status, CreateTime, Name, Level, Number, Composition, YarnCount, Density, GramWeight, Doorframe, Width, Height, Percent
                        FROM products
                        WHERE Status = 1
                        ORDER BY `Level` DESC, `Id` ASC";
            await using var conn = new MySqlConnection(_connectionString);
            var result = await conn.QueryAsync<ImagesVm>(new CommandDefinition(sql, cancellationToken: ct));
            return result.ToList();
        }

        public async Task<ImagesVm?> GetEnabledProductAsync(string guid, CancellationToken ct = default)
        {
            var sql = @"SELECT Id, Guid, Status, CreateTime, Name, Level, Number, Composition, YarnCount, Density, GramWeight, Doorframe, Width, Height, Percent
                        FROM products
                        WHERE Status = 1 AND Guid = @Guid
                        LIMIT 1";
            await using var conn = new MySqlConnection(_connectionString);
            return await conn.QueryFirstOrDefaultAsync<ImagesVm>(new CommandDefinition(sql, new { Guid = guid }, cancellationToken: ct));
        }

        public async Task<MiniUserDto> GetOrCreateUserAsync(string code, string? openId, CancellationToken ct = default)
        {
            var normalizedOpenId = string.IsNullOrWhiteSpace(openId) ? "mock_" + code : openId;
            await using var conn = new MySqlConnection(_connectionString);
            var existing = await conn.QueryFirstOrDefaultAsync<MiniUserDto>(new CommandDefinition(
                "SELECT UserId, OpenId FROM mini_users WHERE OpenId = @OpenId LIMIT 1",
                new { OpenId = normalizedOpenId },
                cancellationToken: ct));
            if (existing != null) return existing;

            var user = new MiniUserDto("user_" + Guid.NewGuid().ToString("N"), normalizedOpenId);
            await conn.ExecuteAsync(new CommandDefinition(
                "INSERT INTO mini_users (UserId, OpenId, Code, CreateTime) VALUES (@UserId, @OpenId, @Code, @CreateTime)",
                new { user.UserId, user.OpenId, Code = code ?? string.Empty, CreateTime = DateTime.Now },
                cancellationToken: ct));
            return user;
        }

        public async Task<(IReadOnlyList<MiniAdminUserDto> Users, int Total)> GetAdminUsersAsync(string? keyword, int current, int pageSize, CancellationToken ct = default)
        {
            NormalizePage(ref current, ref pageSize);
            var filter = BuildKeywordFilter(keyword);
            var whereSql = string.IsNullOrWhiteSpace(filter.Keyword) ? "" : "WHERE UserId LIKE @Keyword OR OpenId LIKE @Keyword OR Code LIKE @Keyword";

            await using var conn = new MySqlConnection(_connectionString);
            var total = await conn.ExecuteScalarAsync<int>(new CommandDefinition(
                "SELECT COUNT(1) FROM mini_users " + whereSql,
                filter,
                cancellationToken: ct));
            var users = await conn.QueryAsync<MiniAdminUserDto>(new CommandDefinition(
                @"SELECT u.UserId,
                         u.OpenId,
                         u.Code,
                         u.CreateTime,
                         COALESCE(c.CartCount, 0) AS CartCount,
                         COALESCE(f.FavoriteCount, 0) AS FavoriteCount,
                         COALESCE(a.AddressCount, 0) AS AddressCount,
                         COALESCE(o.OrderCount, 0) AS OrderCount
                  FROM mini_users u
                  LEFT JOIN (SELECT UserId, COUNT(1) AS CartCount FROM mini_cart_items GROUP BY UserId) c ON c.UserId = u.UserId
                  LEFT JOIN (SELECT UserId, COUNT(1) AS FavoriteCount FROM mini_favorites GROUP BY UserId) f ON f.UserId = u.UserId
                  LEFT JOIN (SELECT UserId, COUNT(1) AS AddressCount FROM mini_addresses GROUP BY UserId) a ON a.UserId = u.UserId
                  LEFT JOIN (SELECT UserId, COUNT(1) AS OrderCount FROM mini_orders GROUP BY UserId) o ON o.UserId = u.UserId
                  " + (string.IsNullOrWhiteSpace(filter.Keyword) ? "" : "WHERE u.UserId LIKE @Keyword OR u.OpenId LIKE @Keyword OR u.Code LIKE @Keyword") + @"
                  ORDER BY u.CreateTime DESC
                  LIMIT @Offset, @Limit",
                new { filter.Keyword, Offset = (current - 1) * pageSize, Limit = pageSize },
                cancellationToken: ct));
            return (users.ToList(), total);
        }

        public async Task<(IReadOnlyList<MiniAdminCartItemDto> Items, int Total)> GetAdminCartItemsAsync(string? keyword, int current, int pageSize, CancellationToken ct = default)
        {
            NormalizePage(ref current, ref pageSize);
            var filter = BuildKeywordFilter(keyword);
            var whereSql = string.IsNullOrWhiteSpace(filter.Keyword)
                ? ""
                : "WHERE c.UserId LIKE @Keyword OR c.ProductGuid LIKE @Keyword OR p.Number LIKE @Keyword OR p.Name LIKE @Keyword";

            await using var conn = new MySqlConnection(_connectionString);
            var total = await conn.ExecuteScalarAsync<int>(new CommandDefinition(
                @"SELECT COUNT(1)
                  FROM mini_cart_items c
                  LEFT JOIN products p ON p.Guid = c.ProductGuid " + whereSql,
                filter,
                cancellationToken: ct));
            var items = await conn.QueryAsync<MiniAdminCartItemDto>(new CommandDefinition(
                @"SELECT c.UserId,
                         c.ProductGuid,
                         c.Quantity,
                         c.Selected,
                         c.UpdateTime,
                         COALESCE(p.Number, '') AS ProductNumber,
                         COALESCE(p.Name, '') AS ProductName,
                         COALESCE(p.Composition, '') AS Composition
                  FROM mini_cart_items c
                  LEFT JOIN products p ON p.Guid = c.ProductGuid
                  " + whereSql + @"
                  ORDER BY c.UpdateTime DESC
                  LIMIT @Offset, @Limit",
                new { filter.Keyword, Offset = (current - 1) * pageSize, Limit = pageSize },
                cancellationToken: ct));
            return (items.ToList(), total);
        }

        public async Task<(IReadOnlyList<MiniAdminFavoriteDto> Items, int Total)> GetAdminFavoritesAsync(string? keyword, int current, int pageSize, CancellationToken ct = default)
        {
            NormalizePage(ref current, ref pageSize);
            var filter = BuildKeywordFilter(keyword);
            var whereSql = string.IsNullOrWhiteSpace(filter.Keyword)
                ? ""
                : "WHERE f.UserId LIKE @Keyword OR f.ProductGuid LIKE @Keyword OR p.Number LIKE @Keyword OR p.Name LIKE @Keyword";

            await using var conn = new MySqlConnection(_connectionString);
            var total = await conn.ExecuteScalarAsync<int>(new CommandDefinition(
                @"SELECT COUNT(1)
                  FROM mini_favorites f
                  LEFT JOIN products p ON p.Guid = f.ProductGuid " + whereSql,
                filter,
                cancellationToken: ct));
            var items = await conn.QueryAsync<MiniAdminFavoriteDto>(new CommandDefinition(
                @"SELECT f.UserId,
                         f.ProductGuid,
                         f.CreateTime,
                         COALESCE(p.Number, '') AS ProductNumber,
                         COALESCE(p.Name, '') AS ProductName,
                         COALESCE(p.Composition, '') AS Composition
                  FROM mini_favorites f
                  LEFT JOIN products p ON p.Guid = f.ProductGuid
                  " + whereSql + @"
                  ORDER BY f.CreateTime DESC
                  LIMIT @Offset, @Limit",
                new { filter.Keyword, Offset = (current - 1) * pageSize, Limit = pageSize },
                cancellationToken: ct));
            return (items.ToList(), total);
        }

        public async Task<(IReadOnlyList<MiniAdminAddressDto> Items, int Total)> GetAdminAddressesAsync(string? keyword, int current, int pageSize, CancellationToken ct = default)
        {
            NormalizePage(ref current, ref pageSize);
            var filter = BuildKeywordFilter(keyword);
            var whereSql = string.IsNullOrWhiteSpace(filter.Keyword)
                ? ""
                : "WHERE UserId LIKE @Keyword OR Name LIKE @Keyword OR Phone LIKE @Keyword OR Region LIKE @Keyword OR Detail LIKE @Keyword";

            await using var conn = new MySqlConnection(_connectionString);
            var total = await conn.ExecuteScalarAsync<int>(new CommandDefinition(
                "SELECT COUNT(1) FROM mini_addresses " + whereSql,
                filter,
                cancellationToken: ct));
            var items = await conn.QueryAsync<MiniAdminAddressDto>(new CommandDefinition(
                @"SELECT Id, UserId, Name, Phone, Region, Detail, IsDefault
                  FROM mini_addresses
                  " + whereSql + @"
                  ORDER BY IsDefault DESC, Id ASC
                  LIMIT @Offset, @Limit",
                new { filter.Keyword, Offset = (current - 1) * pageSize, Limit = pageSize },
                cancellationToken: ct));
            return (items.ToList(), total);
        }

        public async Task<IReadOnlyList<string>> GetFavoriteGuidsAsync(string userId, CancellationToken ct = default)
        {
            await using var conn = new MySqlConnection(_connectionString);
            var result = await conn.QueryAsync<string>(new CommandDefinition(
                "SELECT ProductGuid FROM mini_favorites WHERE UserId = @UserId ORDER BY CreateTime DESC",
                new { UserId = userId },
                cancellationToken: ct));
            return result.ToList();
        }

        public async Task AddFavoriteAsync(string userId, string productGuid, CancellationToken ct = default)
        {
            await using var conn = new MySqlConnection(_connectionString);
            await conn.ExecuteAsync(new CommandDefinition(
                "INSERT IGNORE INTO mini_favorites (UserId, ProductGuid, CreateTime) VALUES (@UserId, @ProductGuid, @CreateTime)",
                new { UserId = userId, ProductGuid = productGuid, CreateTime = DateTime.Now },
                cancellationToken: ct));
        }

        public async Task DeleteFavoriteAsync(string userId, string productGuid, CancellationToken ct = default)
        {
            await using var conn = new MySqlConnection(_connectionString);
            await conn.ExecuteAsync(new CommandDefinition(
                "DELETE FROM mini_favorites WHERE UserId = @UserId AND ProductGuid = @ProductGuid",
                new { UserId = userId, ProductGuid = productGuid },
                cancellationToken: ct));
        }

        public async Task<IReadOnlyList<MiniCartItemDto>> GetCartAsync(string userId, CancellationToken ct = default)
        {
            await using var conn = new MySqlConnection(_connectionString);
            var result = await conn.QueryAsync<MiniCartItemDto>(new CommandDefinition(
                "SELECT ProductGuid, Quantity, Selected FROM mini_cart_items WHERE UserId = @UserId ORDER BY UpdateTime DESC",
                new { UserId = userId },
                cancellationToken: ct));
            return result.ToList();
        }

        public async Task UpsertCartItemAsync(string userId, string productGuid, int quantity, bool selected, CancellationToken ct = default)
        {
            await using var conn = new MySqlConnection(_connectionString);
            await conn.ExecuteAsync(new CommandDefinition(
                @"INSERT INTO mini_cart_items (UserId, ProductGuid, Quantity, Selected, UpdateTime)
                  VALUES (@UserId, @ProductGuid, @Quantity, @Selected, @UpdateTime)
                  ON DUPLICATE KEY UPDATE Quantity = @Quantity, Selected = @Selected, UpdateTime = @UpdateTime",
                new { UserId = userId, ProductGuid = productGuid, Quantity = quantity, Selected = selected, UpdateTime = DateTime.Now },
                cancellationToken: ct));
        }

        public async Task DeleteCartItemAsync(string userId, string productGuid, CancellationToken ct = default)
        {
            await using var conn = new MySqlConnection(_connectionString);
            await conn.ExecuteAsync(new CommandDefinition(
                "DELETE FROM mini_cart_items WHERE UserId = @UserId AND ProductGuid = @ProductGuid",
                new { UserId = userId, ProductGuid = productGuid },
                cancellationToken: ct));
        }

        public async Task<IReadOnlyList<MiniAddressDto>> GetAddressesAsync(string userId, CancellationToken ct = default)
        {
            await using var conn = new MySqlConnection(_connectionString);
            var result = await conn.QueryAsync<MiniAddressDto>(new CommandDefinition(
                "SELECT Id, Name, Phone, Region, Detail, IsDefault FROM mini_addresses WHERE UserId = @UserId ORDER BY IsDefault DESC, Id ASC",
                new { UserId = userId },
                cancellationToken: ct));
            return result.ToList();
        }

        public async Task<MiniAddressDto> SaveAddressAsync(string userId, MiniAddressDto address, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(address.Id))
            {
                address.Id = "addr_" + Guid.NewGuid().ToString("N");
            }

            await using var conn = new MySqlConnection(_connectionString);
            if (address.IsDefault)
            {
                await conn.ExecuteAsync(new CommandDefinition(
                    "UPDATE mini_addresses SET IsDefault = 0 WHERE UserId = @UserId",
                    new { UserId = userId },
                    cancellationToken: ct));
            }

            await conn.ExecuteAsync(new CommandDefinition(
                @"INSERT INTO mini_addresses (Id, UserId, Name, Phone, Region, Detail, IsDefault)
                  VALUES (@Id, @UserId, @Name, @Phone, @Region, @Detail, @IsDefault)
                  ON DUPLICATE KEY UPDATE Name = @Name, Phone = @Phone, Region = @Region, Detail = @Detail, IsDefault = @IsDefault",
                new { address.Id, UserId = userId, address.Name, address.Phone, address.Region, address.Detail, address.IsDefault },
                cancellationToken: ct));

            await EnsureOneDefaultAddressAsync(conn, userId, ct);
            return address;
        }

        public async Task DeleteAddressAsync(string userId, string addressId, CancellationToken ct = default)
        {
            await using var conn = new MySqlConnection(_connectionString);
            await conn.ExecuteAsync(new CommandDefinition(
                "DELETE FROM mini_addresses WHERE UserId = @UserId AND Id = @Id",
                new { UserId = userId, Id = addressId },
                cancellationToken: ct));
            await EnsureOneDefaultAddressAsync(conn, userId, ct);
        }

        public async Task<MiniOrderDto> CreateOrderAsync(string userId, MiniOrderDto order, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(order.Id))
            {
                order.Id = "order_" + DateTimeOffset.Now.ToUnixTimeMilliseconds() + "_" + Guid.NewGuid().ToString("N")[..8];
            }

            order.UserId = userId;
            order.CreateTime = order.CreateTime == default ? DateTime.Now : order.CreateTime;
            await using var conn = new MySqlConnection(_connectionString);
            await conn.ExecuteAsync(new CommandDefinition(
                @"INSERT INTO mini_orders (Id, UserId, Status, AddressJson, ItemsJson, Subtotal, Freight, Total, DeliveryMethod, InvoiceType, CreateTime, PayTime)
                  VALUES (@Id, @UserId, @Status, @AddressJson, @ItemsJson, @Subtotal, @Freight, @Total, @DeliveryMethod, @InvoiceType, @CreateTime, @PayTime)",
                new
                {
                    order.Id,
                    order.UserId,
                    order.Status,
                    AddressJson = JsonSerializer.Serialize(order.Address, JsonOptions),
                    ItemsJson = JsonSerializer.Serialize(order.Items, JsonOptions),
                    order.Subtotal,
                    order.Freight,
                    order.Total,
                    order.DeliveryMethod,
                    order.InvoiceType,
                    order.CreateTime,
                    order.PayTime
                },
                cancellationToken: ct));
            return order;
        }

        public async Task<IReadOnlyList<MiniOrderDto>> GetOrdersAsync(string userId, string? status, CancellationToken ct = default)
        {
            var sql = @"SELECT Id, UserId, Status, AddressJson, ItemsJson, Subtotal, Freight, Total, DeliveryMethod, InvoiceType, CreateTime, PayTime
                        FROM mini_orders
                        WHERE UserId = @UserId";
            if (!string.IsNullOrWhiteSpace(status) && status != "all")
            {
                sql += " AND Status = @Status";
            }
            sql += " ORDER BY CreateTime DESC";

            await using var conn = new MySqlConnection(_connectionString);
            var rows = await conn.QueryAsync<MiniOrderRow>(new CommandDefinition(sql, new { UserId = userId, Status = status }, cancellationToken: ct));
            return rows.Select(ToOrderDto).ToList();
        }

        public async Task<(IReadOnlyList<MiniOrderDto> Orders, int Total)> GetAdminOrdersAsync(string? status, int current, int pageSize, CancellationToken ct = default)
        {
            if (current < 1) current = 1;
            if (pageSize < 1) pageSize = 12;
            if (pageSize > 100) pageSize = 100;

            var whereSql = "";
            if (!string.IsNullOrWhiteSpace(status) && status != "all")
            {
                whereSql = "WHERE Status = @Status";
            }

            await using var conn = new MySqlConnection(_connectionString);
            var total = await conn.ExecuteScalarAsync<int>(new CommandDefinition(
                "SELECT COUNT(1) FROM mini_orders " + whereSql,
                new { Status = status },
                cancellationToken: ct));

            var rows = await conn.QueryAsync<MiniOrderRow>(new CommandDefinition(
                @"SELECT Id, UserId, Status, AddressJson, ItemsJson, Subtotal, Freight, Total, DeliveryMethod, InvoiceType, CreateTime, PayTime
                  FROM mini_orders " + whereSql + @"
                  ORDER BY CreateTime DESC
                  LIMIT @Offset, @Limit",
                new { Status = status, Offset = (current - 1) * pageSize, Limit = pageSize },
                cancellationToken: ct));

            return (rows.Select(ToOrderDto).ToList(), total);
        }

        public async Task<MiniOrderDto?> GetOrderAsync(string userId, string orderId, CancellationToken ct = default)
        {
            await using var conn = new MySqlConnection(_connectionString);
            var row = await conn.QueryFirstOrDefaultAsync<MiniOrderRow>(new CommandDefinition(
                @"SELECT Id, UserId, Status, AddressJson, ItemsJson, Subtotal, Freight, Total, DeliveryMethod, InvoiceType, CreateTime, PayTime
                  FROM mini_orders
                  WHERE UserId = @UserId AND Id = @Id
                  LIMIT 1",
                new { UserId = userId, Id = orderId },
                cancellationToken: ct));
            return row == null ? null : ToOrderDto(row);
        }

        public async Task<MiniOrderDto?> MarkOrderPaidAsync(string userId, string orderId, CancellationToken ct = default)
        {
            await using var conn = new MySqlConnection(_connectionString);
            await conn.ExecuteAsync(new CommandDefinition(
                "UPDATE mini_orders SET Status = 'pendingShip', PayTime = @PayTime WHERE UserId = @UserId AND Id = @Id AND Status = 'pendingPay'",
                new { UserId = userId, Id = orderId, PayTime = DateTime.Now },
                cancellationToken: ct));
            return await GetOrderAsync(userId, orderId, ct);
        }

        public async Task<bool> UpdateOrderStatusAsync(string orderId, string status, CancellationToken ct = default)
        {
            await using var conn = new MySqlConnection(_connectionString);
            var rows = await conn.ExecuteAsync(new CommandDefinition(
                @"UPDATE mini_orders
                  SET Status = @Status,
                      PayTime = CASE WHEN @Status = 'pendingShip' AND PayTime IS NULL THEN @PayTime ELSE PayTime END
                  WHERE Id = @Id",
                new { Id = orderId, Status = status, PayTime = DateTime.Now },
                cancellationToken: ct));
            return rows > 0;
        }

        private static async Task EnsureOneDefaultAddressAsync(MySqlConnection conn, string userId, CancellationToken ct)
        {
            var defaultCount = await conn.ExecuteScalarAsync<int>(new CommandDefinition(
                "SELECT COUNT(1) FROM mini_addresses WHERE UserId = @UserId AND IsDefault = 1",
                new { UserId = userId },
                cancellationToken: ct));
            if (defaultCount > 0) return;

            await conn.ExecuteAsync(new CommandDefinition(
                @"UPDATE mini_addresses
                  SET IsDefault = 1
                  WHERE UserId = @UserId
                  ORDER BY Id ASC
                  LIMIT 1",
                new { UserId = userId },
                cancellationToken: ct));
        }

        private static void NormalizePage(ref int current, ref int pageSize)
        {
            if (current < 1) current = 1;
            if (pageSize < 1) pageSize = 12;
            if (pageSize > 100) pageSize = 100;
        }

        private static KeywordFilter BuildKeywordFilter(string? keyword)
        {
            var value = string.IsNullOrWhiteSpace(keyword) ? null : "%" + keyword.Trim() + "%";
            return new KeywordFilter(value);
        }

        private sealed record KeywordFilter(string? Keyword);

        private static MiniOrderDto ToOrderDto(MiniOrderRow row)
        {
            return new MiniOrderDto
            {
                Id = row.Id,
                UserId = row.UserId,
                Status = row.Status,
                Address = JsonSerializer.Deserialize<MiniAddressDto>(row.AddressJson, JsonOptions),
                Items = JsonSerializer.Deserialize<List<MiniOrderItemDto>>(row.ItemsJson, JsonOptions) ?? [],
                Subtotal = row.Subtotal,
                Freight = row.Freight,
                Total = row.Total,
                DeliveryMethod = row.DeliveryMethod,
                InvoiceType = row.InvoiceType,
                CreateTime = row.CreateTime,
                PayTime = row.PayTime
            };
        }

        private sealed class MiniOrderRow
        {
            public string Id { get; set; } = string.Empty;
            public string UserId { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public string AddressJson { get; set; } = "{}";
            public string ItemsJson { get; set; } = "[]";
            public decimal Subtotal { get; set; }
            public decimal Freight { get; set; }
            public decimal Total { get; set; }
            public string DeliveryMethod { get; set; } = string.Empty;
            public string InvoiceType { get; set; } = string.Empty;
            public DateTime CreateTime { get; set; }
            public DateTime? PayTime { get; set; }
        }
    }
}
