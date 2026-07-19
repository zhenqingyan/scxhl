using Dapper;
using henglong.Web.Models;
using MySqlConnector;

namespace henglong.Web.Common
{
    public class MySqlHelper : IMySqlHelper
    {
        private readonly string _connectionString;
        private readonly ILogger<MySqlHelper> _logger;

        public MySqlHelper(IConfiguration configuration, ILogger<MySqlHelper> logger)
        {
            _connectionString = configuration.GetConnectionString("MySql") ?? string.Empty;
            _logger = logger;
        }

        public void EnsureTableCreated()
        {
            var sql = @"
                CREATE TABLE IF NOT EXISTS `products` (
                  `Id`          BIGINT        NOT NULL AUTO_INCREMENT,
                  `Guid`        VARCHAR(64)   NOT NULL,
                  `Status`      TINYINT(1)    NOT NULL DEFAULT 1,
                  `CreateTime`  DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
                  `Name`        VARCHAR(255)  NOT NULL DEFAULT '',
                  `ImageHash`   VARCHAR(64)   NOT NULL DEFAULT '',
                  `Level`       INT           NOT NULL DEFAULT 1,
                  `Number`      VARCHAR(100)  NOT NULL DEFAULT '',
                  `Composition` VARCHAR(255)  NOT NULL DEFAULT '',
                  `YarnCount`   VARCHAR(100)  NOT NULL DEFAULT '',
                  `Density`     VARCHAR(100)  NOT NULL DEFAULT '',
                  `GramWeight`  VARCHAR(100)  NOT NULL DEFAULT '',
                  `Doorframe`   VARCHAR(100)  NOT NULL DEFAULT '',
                  `Width`       INT           NOT NULL DEFAULT 0,
                  `Height`      INT           NOT NULL DEFAULT 0,
                  `Percent`     DECIMAL(10,2) NOT NULL DEFAULT 0.00,
                  PRIMARY KEY (`Id`),
                  UNIQUE KEY `uk_guid` (`Guid`),
                  KEY `idx_image_hash` (`ImageHash`)
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;";

            using var conn = new MySqlConnection(_connectionString);
            conn.Execute(sql);
        }

        public bool InsertOne(ImagesVm entity)
        {
            try
            {
                var sql =
                    @"INSERT INTO products (Guid, Status, CreateTime, Name, ImageHash, Level, Number, Composition, YarnCount, Density, GramWeight, Doorframe, Width, Height, Percent)
                            VALUES (@Guid, @Status, @CreateTime, @Name, @ImageHash, @Level, @Number, @Composition, @YarnCount, @Density, @GramWeight, @Doorframe, @Width, @Height, @Percent)";

                using var conn = new MySqlConnection(_connectionString);
                conn.Execute(sql, entity);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to insert product {Guid}", entity.Guid);
                return false;
            }
        }

        public async Task<IList<ImagesVm>> GetImagesDataAsync(int offset, int limit, bool isFilterInvalid, string sortField = "", string sortOrder = "", CancellationToken ct = default)
        {
            var conditionSql = "";
            if (isFilterInvalid)
            {
                conditionSql = "WHERE status=1";
            }

            var orderSql = "ORDER BY `Level` DESC, `Id` ASC";
            if (string.Equals(sortField, "createTime", StringComparison.OrdinalIgnoreCase))
            {
                orderSql = string.Equals(sortOrder, "asc", StringComparison.OrdinalIgnoreCase)
                    ? "ORDER BY `CreateTime` ASC, `Id` ASC"
                    : "ORDER BY `CreateTime` DESC, `Id` DESC";
            }

            var sql =
                $"SELECT Id, Guid, Status, CreateTime, Name, ImageHash, Level, Number, Composition, YarnCount, Density, GramWeight, Doorframe, Width, Height, Percent FROM products {conditionSql} {orderSql} LIMIT @Offset, @Limit";

            await using var conn = new MySqlConnection(_connectionString);
            var result = await conn.QueryAsync<ImagesVm>(new CommandDefinition(sql, new { Offset = offset, Limit = limit }, cancellationToken: ct));
            return result.ToList();
        }

        public async Task<int> GetTotalCountImagesDataAsync(CancellationToken ct = default)
        {
            var sql = "SELECT COUNT(1) FROM products";
            await using var conn = new MySqlConnection(_connectionString);
            var result = await conn.ExecuteScalarAsync<int>(new CommandDefinition(sql, cancellationToken: ct));
            return result;
        }

        public async Task<bool> ImageHashExistsAsync(string imageHash, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(imageHash))
            {
                return false;
            }

            var sql = "SELECT COUNT(1) FROM products WHERE ImageHash = @ImageHash LIMIT 1";
            await using var conn = new MySqlConnection(_connectionString);
            var result = await conn.ExecuteScalarAsync<int>(new CommandDefinition(sql, new { ImageHash = imageHash }, cancellationToken: ct));
            return result > 0;
        }

        public bool UpdateStatus(string guid, bool status)
        {
            try
            {
                var sql = "UPDATE products SET Status = @Status WHERE Guid = @Guid";

                using var conn = new MySqlConnection(_connectionString);
                var rows = conn.Execute(sql, new { Guid = guid, Status = status });
                return rows > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update status for product {Guid}", guid);
                return false;
            }
        }

        public async Task<bool> UpdateLevelAsync(UpdateLevelVm param, CancellationToken ct = default)
        {
            try
            {
                var sql = @"UPDATE products
                            SET Level = @level, Number = @number, Composition = @composition,
                                YarnCount = @yarnCount, Density = @density, GramWeight = @gramWeight,
                                Doorframe = @doorframe, Width = @width, Height = @height, Percent = @percent
                            WHERE Guid = @guid";

                using var conn = new MySqlConnection(_connectionString);
                var rows = await conn.ExecuteAsync(new CommandDefinition(sql, param, cancellationToken: ct));
                return rows == 1;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update level for product {Guid}", param.guid);
                return false;
            }
        }

        public async Task<bool> UpdateSizeAsync(UpdateSizeVm param, CancellationToken ct = default)
        {
            try
            {
                var sql = "UPDATE products SET Width = @width, Height = @height, Percent = @percent WHERE Guid = @guid";

                using var conn = new MySqlConnection(_connectionString);
                var rows = await conn.ExecuteAsync(new CommandDefinition(sql, param, cancellationToken: ct));
                return rows == 1;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update size for product {Guid}", param.guid);
                return false;
            }
        }

        public async Task<bool> DeleteOneAsync(string guid, CancellationToken ct = default)
        {
            try
            {
                var sql = "DELETE FROM products WHERE Guid = @Guid";

                using var conn = new MySqlConnection(_connectionString);
                var rows = await conn.ExecuteAsync(new CommandDefinition(sql, new { Guid = guid }, cancellationToken: ct));
                return rows == 1;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete product {Guid}", guid);
                return false;
            }
        }

        public async Task<IReadOnlyList<DuplicateImageGroupVm>> GetDuplicateImageGroupsAsync(CancellationToken ct = default)
        {
            var sql = @"
                SELECT Id, Guid, Status, CreateTime, Name, ImageHash, Level, Number, Composition, YarnCount, Density, GramWeight, Doorframe, Width, Height, Percent
                FROM products p
                WHERE p.ImageHash IS NOT NULL
                    AND p.ImageHash <> ''
                    AND EXISTS (
                        SELECT 1
                        FROM products otherRow
                        WHERE otherRow.ImageHash = p.ImageHash
                            AND otherRow.Id <> p.Id
                    )
                ORDER BY p.ImageHash ASC, p.Id ASC";

            await using var conn = new MySqlConnection(_connectionString);
            var rows = await conn.QueryAsync<ImagesVm>(new CommandDefinition(sql, cancellationToken: ct));

            return rows
                .GroupBy(item => item.ImageHash)
                .Select(group =>
                {
                    var ordered = group.OrderBy(item => item.Id).ToList();
                    var keepItem = ordered.FirstOrDefault();
                    return new DuplicateImageGroupVm
                    {
                        Name = keepItem?.Name ?? string.Empty,
                        ImageHash = group.Key,
                        Width = keepItem?.Width ?? 0,
                        Height = keepItem?.Height ?? 0,
                        KeepItem = keepItem,
                        DeleteItems = ordered.Skip(1).ToList()
                    };
                })
                .Where(group => group.KeepItem != null && group.DeleteItems.Count > 0)
                .ToList();
        }

        public async Task<int> CleanDuplicateImagesAsync(CancellationToken ct = default)
        {
            try
            {
                var sql = @"
                    DELETE p
                    FROM products p
                    INNER JOIN products keepRow ON keepRow.ImageHash = p.ImageHash
                        AND keepRow.Id < p.Id
                    WHERE p.ImageHash IS NOT NULL
                        AND p.ImageHash <> ''
                        AND keepRow.ImageHash IS NOT NULL
                        AND keepRow.ImageHash <> ''";

                await using var conn = new MySqlConnection(_connectionString);
                return await conn.ExecuteAsync(new CommandDefinition(sql, cancellationToken: ct));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to clean duplicate products");
                return -1;
            }
        }
    }
}
