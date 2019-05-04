using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using henglong.Web.Models;
using henglong.Web.Common;
using MongoDB.Bson;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using Aliyun.OSS;

namespace henglong.Web.Controllers
{
    public class ProductController : Controller
    {
        private readonly IMongoDbHelper<BsonDocument> _mongodbHelper;
        private OssClient ossClient;
        private readonly string _endPoint = "oss-cn-shanghai-internal.aliyuncs.com";
        //private readonly string _endPoint = "oss-cn-shanghai.aliyuncs.com";
        private readonly string _accessKey = "";
        private readonly string _accessSecret = "";
        private readonly string _bucketName = "henglong";
        private readonly IHostingEnvironment _hostingEnvironment;
        private static List<byte> imgList = null;
        private static Object lockObj = new object();
        public ProductController(IMongoDbHelper<BsonDocument> mongodbHelper,
        IHostingEnvironment hostingEnvironment)
        {
            _mongodbHelper = mongodbHelper;
            _hostingEnvironment = hostingEnvironment;
            ossClient = new OssClient(_endPoint, _accessKey, _accessSecret);
        }
        public IActionResult Index()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> GetImgs([FromBody]QueryVm entity)
        {
            var imgsList = await _mongodbHelper.GetImagesDataAsync();
            var totalCount = imgsList.Count;
            return Json(new
            {
                data = imgsList.OrderByDescending(o => o.Level).Skip((entity.current - 1) * entity.pageSize).Take(entity.pageSize),
                total = totalCount
            });
        }
        [HttpPost]
        public void ExportFile()
        {
            var files = Request.Form.Files;
            foreach (var item in files)
            {
                var fileGuid = Guid.NewGuid().ToString() + ".jpg";
                var bsonDocument = new BsonDocument();
                bsonDocument.Add(new BsonElement("Guid", fileGuid));
                bsonDocument.Add(new BsonElement("Status", true));
                bsonDocument.Add(new BsonElement("CreateTime", DateTime.Now));
                bsonDocument.Add(new BsonElement("Name", item.FileName));
                bsonDocument.Add(new BsonElement("Level", 1));
                _mongodbHelper.InsetOne(bsonDocument);
                ossClient.PutObject(_bucketName, fileGuid, item.OpenReadStream());
                ossClient.SetObjectAcl(_bucketName, fileGuid, CannedAccessControlList.PublicRead);
            }
        }

        [HttpGet]
        public async Task<FileResult> GetImg(string guid)
        {
            var response = ossClient.GetObject(_bucketName, guid);
            using (var responseStream = response.Content)
            {

                using (var memeryStrem = new MemoryStream())
                {
                    await responseStream.CopyToAsync(memeryStrem);
                    var fileLen = (int)responseStream.Length;
                    var fileBytes = new byte[fileLen];
                    memeryStrem.Position = 0;
                    var readlll =await memeryStrem.ReadAsync(fileBytes, 0, fileLen);
                    return new FileContentResult(fileBytes, "image/jpeg");
                }
            }
            /* if (imgList != null)
            {
                return new FileContentResult(imgList.ToArray(), "image/jpeg");
            }

            using (var fileStream = new FileStream(@"C:\Users\Yan\Pictures\GetImg.jpg", FileMode.Open))
            {
                byte[] result = new byte[fileStream.Length];
                await fileStream.ReadAsync(result, 0, (int)fileStream.Length);
                lock (lockObj)
                {
                    imgList = result.ToList();
                }
                return new FileContentResult(result, "image/jpeg");
            }*/
        }

        [HttpPost]
        public JsonResult Update([FromBody]UpdateVm entity)
        {
            var result = _mongodbHelper.UpdateOne(entity.guid, entity.status);
            return Json(result ? "成功" : "失败");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateLevel([FromBody]UpdateLevelVm entity)
        {
            var result = await _mongodbHelper.UpdateLevelAsync(entity.guid, entity.level);
            return Json(result ? "成功" : "失败");
        }

        [HttpPost]
        public async Task<IActionResult> Del([FromBody]DelVm entity)
        {
            var result = await _mongodbHelper.DelOneAsync(entity.guid);
            return Json(result ? 1 : 0);
        }
    }
}
