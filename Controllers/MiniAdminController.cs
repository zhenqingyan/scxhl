using henglong.Web.Common;
using henglong.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace henglong.Web.Controllers
{
    [Authorize]
    public class MiniAdminController : Controller
    {
        private readonly IMiniProgramStore _store;

        public MiniAdminController(IMiniProgramStore store)
        {
            _store = store;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GetUsers([FromBody] MiniAdminQueryVm query, CancellationToken ct)
        {
            NormalizeQuery(query);
            var result = await _store.GetAdminUsersAsync(query.Keyword, query.Current, query.PageSize, ct);
            return Json(new MiniAdminPageDto<MiniAdminUserDto> { Data = result.Users, Total = result.Total });
        }

        [HttpPost]
        public async Task<IActionResult> GetCartItems([FromBody] MiniAdminQueryVm query, CancellationToken ct)
        {
            NormalizeQuery(query);
            var result = await _store.GetAdminCartItemsAsync(query.Keyword, query.Current, query.PageSize, ct);
            return Json(new MiniAdminPageDto<MiniAdminCartItemDto> { Data = result.Items, Total = result.Total });
        }

        [HttpPost]
        public async Task<IActionResult> GetFavorites([FromBody] MiniAdminQueryVm query, CancellationToken ct)
        {
            NormalizeQuery(query);
            var result = await _store.GetAdminFavoritesAsync(query.Keyword, query.Current, query.PageSize, ct);
            return Json(new MiniAdminPageDto<MiniAdminFavoriteDto> { Data = result.Items, Total = result.Total });
        }

        [HttpPost]
        public async Task<IActionResult> GetAddresses([FromBody] MiniAdminQueryVm query, CancellationToken ct)
        {
            NormalizeQuery(query);
            var result = await _store.GetAdminAddressesAsync(query.Keyword, query.Current, query.PageSize, ct);
            return Json(new MiniAdminPageDto<MiniAdminAddressDto> { Data = result.Items, Total = result.Total });
        }

        private static void NormalizeQuery(MiniAdminQueryVm query)
        {
            if (query.Current < 1) query.Current = 1;
            if (query.PageSize < 1) query.PageSize = 12;
            if (query.PageSize > 100) query.PageSize = 100;
            query.Keyword = query.Keyword?.Trim();
        }
    }
}
