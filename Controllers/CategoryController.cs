using henglong.Web.Common;
using Microsoft.AspNetCore.Mvc;

namespace henglong.Web.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class CategoryController : ControllerBase
    {
        private readonly IMiniProgramStore _store;

        public CategoryController(IMiniProgramStore store)
        {
            _store = store;
        }

        [HttpGet]
        public async Task<IActionResult> Get(CancellationToken ct)
        {
            var products = await _store.GetEnabledProductsAsync(ct);
            var categories = products
                .Where(product => !string.IsNullOrWhiteSpace(product.Composition))
                .Select(product => product.Composition.Trim())
                .Distinct()
                .OrderBy(name => name)
                .Select((name, index) => new { id = index + 1, category = name })
                .ToList();

            return Ok(categories);
        }
    }
}
