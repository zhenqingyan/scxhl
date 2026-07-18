namespace henglong.Web.Models
{
    public class AdminLoginVm
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? ReturnUrl { get; set; }
    }

    public class AdminResetPasswordVm
    {
        public string Username { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }

    public class AdminUserCreateVm
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class AdminUserStatusVm
    {
        public string Username { get; set; } = string.Empty;
        public bool IsEnabled { get; set; }
    }

    public class AdminUserListItemDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public bool IsEnabled { get; set; }
        public DateTime CreateTime { get; set; }
        public DateTime UpdateTime { get; set; }
        public DateTime? LastLoginTime { get; set; }
    }

    public class AdminAuthResult
    {
        public bool Succeeded { get; init; }
        public string Username { get; init; } = string.Empty;
        public string AdminType { get; init; } = string.Empty;
        public int? DatabaseAdminId { get; init; }
        public string Message { get; init; } = string.Empty;

        public static AdminAuthResult Success(string username, string adminType, int? databaseAdminId = null)
        {
            return new AdminAuthResult
            {
                Succeeded = true,
                Username = username,
                AdminType = adminType,
                DatabaseAdminId = databaseAdminId
            };
        }

        public static AdminAuthResult Fail(string message)
        {
            return new AdminAuthResult { Succeeded = false, Message = message };
        }
    }
}
