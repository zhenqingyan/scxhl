using Dapper;
using henglong.Web.Models;
using MySqlConnector;

namespace henglong.Web.Common
{
    public class AdminUserStore : IAdminUserStore
    {
        private readonly string _connectionString;
        private readonly ILogger<AdminUserStore> _logger;

        public AdminUserStore(IConfiguration configuration, ILogger<AdminUserStore> logger)
        {
            _connectionString = configuration.GetConnectionString("MySql") ?? string.Empty;
            _logger = logger;
        }

        public void EnsureTableCreated()
        {
            const string sql = @"
                CREATE TABLE IF NOT EXISTS `admin_users` (
                  `Id`            INT          NOT NULL AUTO_INCREMENT,
                  `Username`      VARCHAR(64)  NOT NULL,
                  `PasswordHash`  VARCHAR(512) NOT NULL,
                  `IsEnabled`     TINYINT(1)   NOT NULL DEFAULT 1,
                  `CreateTime`    DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
                  `UpdateTime`    DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
                  `LastLoginTime` DATETIME     NULL,
                  PRIMARY KEY (`Id`),
                  UNIQUE KEY `uk_admin_username` (`Username`)
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;";

            using var conn = new MySqlConnection(_connectionString);
            conn.Execute(sql);
        }

        public async Task<AdminUser?> GetByUsernameAsync(string username, CancellationToken ct = default)
        {
            const string sql = @"SELECT Id, Username, PasswordHash, IsEnabled, CreateTime, UpdateTime, LastLoginTime
                                 FROM admin_users
                                 WHERE Username = @Username
                                 LIMIT 1";
            await using var conn = new MySqlConnection(_connectionString);
            return await conn.QueryFirstOrDefaultAsync<AdminUser>(new CommandDefinition(
                sql,
                new { Username = username },
                cancellationToken: ct));
        }

        public async Task UpdateLastLoginTimeAsync(int id, CancellationToken ct = default)
        {
            const string sql = "UPDATE admin_users SET LastLoginTime = @LastLoginTime WHERE Id = @Id";
            await using var conn = new MySqlConnection(_connectionString);
            await conn.ExecuteAsync(new CommandDefinition(
                sql,
                new { Id = id, LastLoginTime = DateTime.Now },
                cancellationToken: ct));
        }

        public async Task<bool> ResetPasswordAsync(string username, string passwordHash, CancellationToken ct = default)
        {
            const string sql = @"UPDATE admin_users
                                 SET PasswordHash = @PasswordHash,
                                     UpdateTime = @UpdateTime
                                 WHERE Username = @Username";
            await using var conn = new MySqlConnection(_connectionString);
            var rows = await conn.ExecuteAsync(new CommandDefinition(
                sql,
                new { Username = username, PasswordHash = passwordHash, UpdateTime = DateTime.Now },
                cancellationToken: ct));
            return rows > 0;
        }
    }
}
