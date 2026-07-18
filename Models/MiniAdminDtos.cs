namespace henglong.Web.Models
{
    public class MiniAdminQueryVm
    {
        public int Current { get; set; } = 1;
        public int PageSize { get; set; } = 12;
        public string? Keyword { get; set; }
    }

    public class MiniAdminPageDto<T>
    {
        public IReadOnlyList<T> Data { get; set; } = [];
        public int Total { get; set; }
    }

    public class MiniAdminUserDto
    {
        public string UserId { get; set; } = string.Empty;
        public string OpenId { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public DateTime CreateTime { get; set; }
        public int CartCount { get; set; }
        public int FavoriteCount { get; set; }
        public int AddressCount { get; set; }
        public int OrderCount { get; set; }
    }

    public class MiniAdminCartItemDto
    {
        public string UserId { get; set; } = string.Empty;
        public string ProductGuid { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public bool Selected { get; set; }
        public DateTime UpdateTime { get; set; }
        public string ProductNumber { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string Composition { get; set; } = string.Empty;
    }

    public class MiniAdminFavoriteDto
    {
        public string UserId { get; set; } = string.Empty;
        public string ProductGuid { get; set; } = string.Empty;
        public DateTime CreateTime { get; set; }
        public string ProductNumber { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string Composition { get; set; } = string.Empty;
    }

    public class MiniAdminAddressDto
    {
        public string Id { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public string Detail { get; set; } = string.Empty;
        public bool IsDefault { get; set; }
    }
}
