using Dapper;
using henglong.Web.Models;
using MySqlConnector;

namespace henglong.Web.Common
{
    public class MySqlHelper : IMySqlHelper
    {
        private readonly string _connectionString;

        public MySqlHelper(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("MySql") ?? string.Empty;
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
                  UNIQUE KEY `uk_guid` (`Guid`)
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;";

            using (var conn = new MySqlConnection(_connectionString))
            {
                conn.Execute(sql);
            }
        }

        public bool InsertOne(ImgesVm entity)
        {
            try
            {
                var sql = @"INSERT INTO products (Guid, Status, CreateTime, Name, Level, Number, Composition, YarnCount, Density, GramWeight, Doorframe, Width, Height, Percent)
                            VALUES (@Guid, @Status, @CreateTime, @Name, @Level, @Number, @Composition, @YarnCount, @Density, @GramWeight, @Doorframe, @Width, @Height, @Percent)";

                using (var conn = new MySqlConnection(_connectionString))
                {
                    conn.Execute(sql, entity);
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<IList<ImgesVm>> GetImagesDataAsync()
        {
            var sql = "SELECT Id, Guid, Status, CreateTime, Name, Level, Number, Composition, YarnCount, Density, GramWeight, Doorframe, Width, Height, Percent FROM products";

            using (var conn = new MySqlConnection(_connectionString))
            {
                var result = await conn.QueryAsync<ImgesVm>(sql);
                return result.ToList();
            }
        }

        public bool UpdateStatus(string guid, bool status)
        {
            try
            {
                var sql = "UPDATE products SET Status = @Status WHERE Guid = @Guid";

                using (var conn = new MySqlConnection(_connectionString))
                {
                    var rows = conn.Execute(sql, new { Guid = guid, Status = status });
                    return rows > 0;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> UpdateLevelAsync(UpdateLevelVm param)
        {
            try
            {
                var sql = @"UPDATE products 
                            SET Level = @level, Number = @number, Composition = @composition, 
                                YarnCount = @yarnCount, Density = @density, GramWeight = @gramWeight, 
                                Doorframe = @doorframe, Width = @width, Height = @height, Percent = @percent 
                            WHERE Guid = @guid";

                using (var conn = new MySqlConnection(_connectionString))
                {
                    var rows = await conn.ExecuteAsync(sql, param);
                    return rows == 1;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> UpdateSizeAsync(UpdateSizeVm param)
        {
            try
            {
                var sql = "UPDATE products SET Width = @width, Height = @height, Percent = @percent WHERE Guid = @guid";

                using (var conn = new MySqlConnection(_connectionString))
                {
                    var rows = await conn.ExecuteAsync(sql, param);
                    return rows == 1;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> DeleteOneAsync(string guid)
        {
            try
            {
                var sql = "DELETE FROM products WHERE Guid = @Guid";

                using (var conn = new MySqlConnection(_connectionString))
                {
                    var rows = await conn.ExecuteAsync(sql, new { Guid = guid });
                    return rows == 1;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

    }
}
