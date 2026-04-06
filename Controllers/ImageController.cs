using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using henglong.Web.Common;
using henglong.Web.Models;

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
            var imgsList = await _mySqlHelper.GetImagesDataAsync();
            int id = 1;
            foreach (var item in imgsList)
            {
                item.Id = id;
                id++;
            }
            return imgsList.Where(p => p.Status).OrderByDescending(p => p.Level).Skip(index * pageSize).Take(pageSize).ToList();
        }
    }
}
