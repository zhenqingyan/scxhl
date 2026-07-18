using henglong.Web.Common;
using henglong.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace henglong.Web.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private static readonly HashSet<string> AllowedStatuses = new(StringComparer.OrdinalIgnoreCase)
        {
            "pendingPay",
            "pendingShip",
            "shipped",
            "completed",
            "cancelled"
        };

        private readonly IMiniProgramStore _store;

        public OrderController(IMiniProgramStore store)
        {
            _store = store;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GetOrders([FromBody] OrderQueryVm query, CancellationToken ct)
        {
            if (query.Current < 1) query.Current = 1;
            if (query.PageSize < 1) query.PageSize = 12;
            if (query.PageSize > 100) query.PageSize = 100;

            var result = await _store.GetAdminOrdersAsync(query.Status, query.Current, query.PageSize, ct);
            return Json(new AdminOrderPageDto
            {
                Data = result.Orders,
                Total = result.Total
            });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStatus([FromBody] OrderStatusUpdateVm entity, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(entity.OrderId))
            {
                return Json("失败：缺少订单号");
            }

            if (!AllowedStatuses.Contains(entity.Status))
            {
                return Json("失败：无效状态");
            }

            var result = await _store.UpdateOrderStatusAsync(entity.OrderId, entity.Status, ct);
            return Json(result ? "成功" : "失败");
        }
    }
}
