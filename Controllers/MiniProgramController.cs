using henglong.Web.Common;
using henglong.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace henglong.Web.Controllers
{
    [Route("api/mini")]
    [ApiController]
    public class MiniProgramController : ControllerBase
    {
        private const decimal DefaultProductPrice = 30m;
        private const decimal DefaultFreight = 0m;
        private readonly IMiniProgramStore _store;

        public MiniProgramController(IMiniProgramStore store)
        {
            _store = store;
        }

        [HttpGet("categories")]
        public async Task<ActionResult<IReadOnlyList<MiniCategoryDto>>> GetCategories(CancellationToken ct)
        {
            var products = await _store.GetEnabledProductsAsync(ct);
            var categories = products
                .Where(product => !string.IsNullOrWhiteSpace(product.Composition))
                .GroupBy(product => product.Composition.Trim())
                .OrderBy(group => group.Key)
                .Select(group => new MiniCategoryDto
                {
                    Id = group.Key,
                    Name = group.Key,
                    Count = group.Count()
                })
                .ToList();

            categories.Insert(0, new MiniCategoryDto { Id = "all", Name = "全部", Count = products.Count });
            return Ok(categories);
        }

        [HttpGet("products")]
        public async Task<ActionResult<MiniPagedResult<MiniProductDto>>> GetProducts(
            [FromQuery] string? keyword,
            [FromQuery] string? category,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 12,
            CancellationToken ct = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 12;
            if (pageSize > 100) pageSize = 100;

            var query = (await _store.GetEnabledProductsAsync(ct)).AsEnumerable();
            if (!string.IsNullOrWhiteSpace(category) && category != "all")
            {
                query = query.Where(product => string.Equals(product.Composition, category, StringComparison.OrdinalIgnoreCase));
            }

            var word = (keyword ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(word))
            {
                query = query.Where(product => ProductSearchText(product).Contains(word, StringComparison.OrdinalIgnoreCase));
            }

            var ordered = query.OrderByDescending(product => product.Level).ThenBy(product => product.Id).ToList();
            var total = ordered.Count;
            var items = ordered.Skip((page - 1) * pageSize).Take(pageSize).Select(ToProductDto).ToList();

            return Ok(new MiniPagedResult<MiniProductDto>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                Total = total,
                TotalPages = (int)Math.Ceiling(total / (pageSize * 1m))
            });
        }

        [HttpGet("products/{guid}")]
        public async Task<ActionResult<MiniProductDto>> GetProduct(string guid, CancellationToken ct)
        {
            var product = await _store.GetEnabledProductAsync(guid, ct);
            if (product == null) return NotFound(new { message = "商品不存在" });
            return Ok(ToProductDto(product));
        }

        [HttpPost("auth/login")]
        public async Task<ActionResult<MiniLoginResponse>> Login([FromBody] MiniLoginRequest request, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(request.Code) && string.IsNullOrWhiteSpace(request.OpenId))
            {
                return BadRequest(new { message = "缺少登录凭证" });
            }

            var user = await _store.GetOrCreateUserAsync(request.Code, request.OpenId, ct);
            return Ok(new MiniLoginResponse
            {
                UserId = user.UserId,
                OpenId = user.OpenId,
                Token = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(user.UserId + ":" + user.OpenId))
            });
        }

        [HttpGet("favorites")]
        public async Task<ActionResult<IReadOnlyList<MiniProductDto>>> GetFavorites(CancellationToken ct)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized(new { message = "缺少用户标识" });

            var favoriteGuids = await _store.GetFavoriteGuidsAsync(userId, ct);
            var products = await _store.GetEnabledProductsAsync(ct);
            var favorites = products
                .Where(product => favoriteGuids.Contains(product.Guid))
                .Select(ToProductDto)
                .ToList();
            return Ok(favorites);
        }

        [HttpPost("favorites")]
        public async Task<IActionResult> AddFavorite([FromBody] MiniFavoriteRequest request, CancellationToken ct)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized(new { message = "缺少用户标识" });
            if (string.IsNullOrWhiteSpace(request.ProductGuid)) return BadRequest(new { message = "缺少商品标识" });

            await _store.AddFavoriteAsync(userId, request.ProductGuid, ct);
            return Ok(new { message = "成功" });
        }

        [HttpDelete("favorites/{productGuid}")]
        public async Task<IActionResult> DeleteFavorite(string productGuid, CancellationToken ct)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized(new { message = "缺少用户标识" });

            await _store.DeleteFavoriteAsync(userId, productGuid, ct);
            return Ok(new { message = "成功" });
        }

        [HttpGet("cart")]
        public async Task<ActionResult<MiniCartDto>> GetCart(CancellationToken ct)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized(new { message = "缺少用户标识" });

            return Ok(await BuildCartAsync(userId, ct));
        }

        [HttpPost("cart")]
        public async Task<IActionResult> AddCartItem([FromBody] MiniCartUpsertRequest request, CancellationToken ct)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized(new { message = "缺少用户标识" });
            if (string.IsNullOrWhiteSpace(request.ProductGuid)) return BadRequest(new { message = "缺少商品标识" });

            await _store.UpsertCartItemAsync(userId, request.ProductGuid, NormalizeQuantity(request.Quantity), request.Selected, ct);
            return Ok(new { message = "成功" });
        }

        [HttpPatch("cart/{productGuid}")]
        public async Task<IActionResult> UpdateCartItem(string productGuid, [FromBody] MiniCartUpdateRequest request, CancellationToken ct)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized(new { message = "缺少用户标识" });

            await _store.UpsertCartItemAsync(userId, productGuid, NormalizeQuantity(request.Quantity), request.Selected, ct);
            return Ok(new { message = "成功" });
        }

        [HttpDelete("cart/{productGuid}")]
        public async Task<IActionResult> DeleteCartItem(string productGuid, CancellationToken ct)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized(new { message = "缺少用户标识" });

            await _store.DeleteCartItemAsync(userId, productGuid, ct);
            return Ok(new { message = "成功" });
        }

        [HttpGet("addresses")]
        public async Task<ActionResult<IReadOnlyList<MiniAddressDto>>> GetAddresses(CancellationToken ct)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized(new { message = "缺少用户标识" });

            return Ok(await _store.GetAddressesAsync(userId, ct));
        }

        [HttpPost("addresses")]
        public async Task<ActionResult<MiniAddressDto>> SaveAddress([FromBody] MiniAddressRequest request, CancellationToken ct)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized(new { message = "缺少用户标识" });

            var address = new MiniAddressDto
            {
                Id = request.Id ?? string.Empty,
                Name = request.Name,
                Phone = request.Phone,
                Region = request.Region,
                Detail = request.Detail,
                IsDefault = request.IsDefault
            };

            return Ok(await _store.SaveAddressAsync(userId, address, ct));
        }

        [HttpPut("addresses/{addressId}")]
        public async Task<ActionResult<MiniAddressDto>> UpdateAddress(string addressId, [FromBody] MiniAddressRequest request, CancellationToken ct)
        {
            request.Id = addressId;
            return await SaveAddress(request, ct);
        }

        [HttpDelete("addresses/{addressId}")]
        public async Task<IActionResult> DeleteAddress(string addressId, CancellationToken ct)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized(new { message = "缺少用户标识" });

            await _store.DeleteAddressAsync(userId, addressId, ct);
            return Ok(new { message = "成功" });
        }

        [HttpPost("orders")]
        public async Task<ActionResult<MiniOrderDto>> CreateOrder([FromBody] MiniCreateOrderRequest request, CancellationToken ct)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized(new { message = "缺少用户标识" });
            if (request.Items.Count == 0) return BadRequest(new { message = "订单商品不能为空" });

            var addresses = await _store.GetAddressesAsync(userId, ct);
            var address = addresses.FirstOrDefault(item => item.Id == request.AddressId);
            if (address == null) return BadRequest(new { message = "收货地址不存在" });

            var orderItems = new List<MiniOrderItemDto>();
            foreach (var item in request.Items)
            {
                var product = await _store.GetEnabledProductAsync(item.ProductGuid, ct);
                if (product == null) return BadRequest(new { message = "商品不存在" });

                var productDto = ToProductDto(product);
                var quantity = NormalizeQuantity(item.Quantity);
                orderItems.Add(new MiniOrderItemDto
                {
                    ProductGuid = product.Guid,
                    Number = productDto.Number,
                    Quantity = quantity,
                    Price = productDto.Price,
                    Amount = productDto.Price * quantity,
                    Product = productDto
                });
            }

            var subtotal = orderItems.Sum(item => item.Amount);
            var order = new MiniOrderDto
            {
                UserId = userId,
                Status = "pendingPay",
                Address = address,
                Items = orderItems,
                Subtotal = subtotal,
                Freight = DefaultFreight,
                Total = subtotal + DefaultFreight,
                DeliveryMethod = request.DeliveryMethod,
                InvoiceType = request.InvoiceType,
                CreateTime = DateTime.Now
            };

            return Ok(await _store.CreateOrderAsync(userId, order, ct));
        }

        [HttpGet("orders")]
        public async Task<ActionResult<IReadOnlyList<MiniOrderDto>>> GetOrders([FromQuery] string? status, CancellationToken ct)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized(new { message = "缺少用户标识" });

            return Ok(await _store.GetOrdersAsync(userId, status, ct));
        }

        [HttpGet("orders/{orderId}")]
        public async Task<ActionResult<MiniOrderDto>> GetOrder(string orderId, CancellationToken ct)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized(new { message = "缺少用户标识" });

            var order = await _store.GetOrderAsync(userId, orderId, ct);
            if (order == null) return NotFound(new { message = "订单不存在" });
            return Ok(order);
        }

        [HttpPost("orders/{orderId}/pay")]
        public async Task<ActionResult<MiniOrderDto>> PayOrder(string orderId, CancellationToken ct)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized(new { message = "缺少用户标识" });

            var order = await _store.MarkOrderPaidAsync(userId, orderId, ct);
            if (order == null) return NotFound(new { message = "订单不存在" });
            return Ok(order);
        }

        private async Task<MiniCartDto> BuildCartAsync(string userId, CancellationToken ct)
        {
            var items = (await _store.GetCartAsync(userId, ct)).ToList();
            foreach (var item in items)
            {
                var product = await _store.GetEnabledProductAsync(item.ProductGuid, ct);
                item.Product = product == null ? null : ToProductDto(product);
            }

            return new MiniCartDto
            {
                Items = items,
                SelectedCount = items.Where(item => item.Selected).Sum(item => item.Quantity),
                Subtotal = items.Where(item => item.Selected && item.Product != null).Sum(item => item.Product!.Price * item.Quantity)
            };
        }

        private string? GetUserId()
        {
            var value = Request.Headers["X-Mini-User-Id"].FirstOrDefault();
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        private static int NormalizeQuantity(int quantity)
        {
            return quantity < 1 ? 1 : quantity;
        }

        private static MiniProductDto ToProductDto(ImagesVm product)
        {
            return new MiniProductDto
            {
                Guid = product.Guid,
                Number = string.IsNullOrWhiteSpace(product.Number) ? Path.GetFileNameWithoutExtension(product.Name) : product.Number,
                Name = product.Name,
                ImageUrl = "https://henglong.oss-cn-shanghai.aliyuncs.com/" + product.Guid,
                Composition = product.Composition,
                YarnCount = product.YarnCount,
                Density = product.Density,
                GramWeight = product.GramWeight,
                Doorframe = product.Doorframe,
                Width = product.Width,
                Height = product.Height,
                Percent = product.Percent,
                Level = product.Level,
                Price = DefaultProductPrice,
                Unit = "元/米"
            };
        }

        private static string ProductSearchText(ImagesVm product)
        {
            return string.Join(' ', product.Guid, product.Name, product.Number, product.Composition, product.YarnCount, product.Density, product.GramWeight, product.Doorframe);
        }
    }
}
