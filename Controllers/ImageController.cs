using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using henglong.Web.Common;
using henglong.Web.Models;

namespace henglong.Web.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class ImageController : ControllerBase
    {
        private readonly IMongoDbHelper<BsonDocument> _mongodbHelper;
        public ImageController(IMongoDbHelper<BsonDocument> mongodbHelper)
        {
            _mongodbHelper = mongodbHelper;
        }
        [HttpGet]
        public async Task<IList<ImgesVm>> Get(int index = 0, int pageSize = 9)
        {
            var imgsList = await _mongodbHelper.GetImagesDataAsync();
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