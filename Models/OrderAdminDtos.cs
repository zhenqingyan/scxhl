namespace henglong.Web.Models
{
    public class OrderQueryVm
    {
        public int Current { get; set; } = 1;
        public int PageSize { get; set; } = 12;
        public string? Status { get; set; }
    }

    public class OrderStatusUpdateVm
    {
        public string OrderId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class AdminOrderPageDto
    {
        public IReadOnlyList<MiniOrderDto> Data { get; set; } = [];
        public int Total { get; set; }
    }
}
