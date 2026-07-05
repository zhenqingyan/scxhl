using henglong.Web.Common;
using henglong.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace henglong.Web.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class ImageController : ControllerBase
    {
        private readonly IMySqlHelper _mySqlHelper;

        public ImageController(IMySqlHelper mySqlHelper)
        {
            _mySqlHelper = mySqlHelper;
        }

        [HttpGet]
        public async Task<IList<ImagesVm>> Get(int index = 0, int pageSize = 9, CancellationToken ct = default)
        {
            // Validate pagination params
            if (index < 0) index = 0;
            if (pageSize < 1) pageSize = 9;
            if (pageSize > 100) pageSize = 100;

            var imgsList = await _mySqlHelper.GetImagesDataAsync(index * pageSize, pageSize, true, ct);
            return imgsList.ToList();
        }
    }
}
