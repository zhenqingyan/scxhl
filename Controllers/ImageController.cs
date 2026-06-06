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
        public async Task<IList<ImgesVm>> Get(int index = 0, int pageSize = 9)
        {
            var imgsList = await _mySqlHelper.GetImagesDataAsync(index*pageSize,pageSize,true);
            return imgsList.ToList();
        }
    }
}
