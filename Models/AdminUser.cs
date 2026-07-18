namespace henglong.Web.Models
{
    public class AdminUser
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public bool IsEnabled { get; set; }
        public DateTime CreateTime { get; set; }
        public DateTime UpdateTime { get; set; }
        public DateTime? LastLoginTime { get; set; }
    }

    public static class AdminIdentityTypes
    {
        public const string Super = "Super";
        public const string Database = "Database";
    }

    public static class AdminClaimTypes
    {
        public const string AdminType = "AdminType";
    }
}
