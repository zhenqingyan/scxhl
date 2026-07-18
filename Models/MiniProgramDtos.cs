namespace henglong.Web.Models
{
    public class MiniCategoryDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class MiniPagedResult<T>
    {
        public IReadOnlyList<T> Items { get; set; } = [];
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
        public int TotalPages { get; set; }
    }

    public class MiniProductDto
    {
        public string Guid { get; set; } = string.Empty;
        public string Number { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string Composition { get; set; } = string.Empty;
        public string YarnCount { get; set; } = string.Empty;
        public string Density { get; set; } = string.Empty;
        public string GramWeight { get; set; } = string.Empty;
        public string Doorframe { get; set; } = string.Empty;
        public int Width { get; set; }
        public int Height { get; set; }
        public decimal Percent { get; set; }
        public int Level { get; set; }
        public decimal Price { get; set; } = 30m;
        public string Unit { get; set; } = "元/米";
    }

    public class MiniLoginRequest
    {
        public string Code { get; set; } = string.Empty;
        public string? OpenId { get; set; }
    }

    public class MiniUserDto
    {
        public MiniUserDto()
        {
        }

        public MiniUserDto(string userId, string openId)
        {
            UserId = userId;
            OpenId = openId;
        }

        public string UserId { get; set; } = string.Empty;
        public string OpenId { get; set; } = string.Empty;
    }

    public class MiniLoginResponse
    {
        public string UserId { get; set; } = string.Empty;
        public string OpenId { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
    }

    public class MiniFavoriteRequest
    {
        public string ProductGuid { get; set; } = string.Empty;
    }

    public class MiniCartUpsertRequest
    {
        public string ProductGuid { get; set; } = string.Empty;
        public int Quantity { get; set; } = 1;
        public bool Selected { get; set; } = true;
    }

    public class MiniCartUpdateRequest
    {
        public int Quantity { get; set; } = 1;
        public bool Selected { get; set; } = true;
    }

    public class MiniCartItemDto
    {
        public MiniCartItemDto()
        {
        }

        public MiniCartItemDto(string productGuid, int quantity, bool selected)
        {
            ProductGuid = productGuid;
            Quantity = quantity;
            Selected = selected;
        }

        public string ProductGuid { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public bool Selected { get; set; }
        public MiniProductDto? Product { get; set; }
    }

    public class MiniCartDto
    {
        public IReadOnlyList<MiniCartItemDto> Items { get; set; } = [];
        public int SelectedCount { get; set; }
        public decimal Subtotal { get; set; }
    }

    public class MiniAddressRequest
    {
        public string? Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public string Detail { get; set; } = string.Empty;
        public bool IsDefault { get; set; }
    }

    public class MiniAddressDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public string Detail { get; set; } = string.Empty;
        public bool IsDefault { get; set; }
    }

    public class MiniOrderItemRequest
    {
        public string ProductGuid { get; set; } = string.Empty;
        public int Quantity { get; set; } = 1;
    }

    public class MiniCreateOrderRequest
    {
        public string AddressId { get; set; } = string.Empty;
        public IReadOnlyList<MiniOrderItemRequest> Items { get; set; } = [];
        public string DeliveryMethod { get; set; } = "sf";
        public string InvoiceType { get; set; } = "none";
    }

    public class MiniOrderItemDto
    {
        public string ProductGuid { get; set; } = string.Empty;
        public string Number { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal Amount { get; set; }
        public MiniProductDto? Product { get; set; }
    }

    public class MiniOrderDto
    {
        public string Id { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string Status { get; set; } = "pendingPay";
        public MiniAddressDto? Address { get; set; }
        public IReadOnlyList<MiniOrderItemDto> Items { get; set; } = [];
        public decimal Subtotal { get; set; }
        public decimal Freight { get; set; }
        public decimal Total { get; set; }
        public string DeliveryMethod { get; set; } = "sf";
        public string InvoiceType { get; set; } = "none";
        public DateTime CreateTime { get; set; } = DateTime.Now;
        public DateTime? PayTime { get; set; }
    }
}
