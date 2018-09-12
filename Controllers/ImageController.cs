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
        public IList<ImgesVm> Get(int index=0,int pageSize=20)
        {
            var imgsList = _mongodbHelper.GetData();
            var result = new List<ImgesVm>();
            int id=1;
            foreach (var item in imgsList)
            {
                var imgInfo = new ImgesVm()
                {
                    id=id,
                    guid = item.GetValue(1).ToString(),
                    status = item.GetValue(2).ToBoolean()
                };
                id++;
                if(imgInfo.status)
                {
                    result.Add(imgInfo);
                }
            }
            return result.Skip(index*pageSize).Take(pageSize).ToList();
        }
    }
}