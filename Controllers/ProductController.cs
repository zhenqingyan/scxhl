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
        [HttpGet]
        public JsonResult GetImgs()
        {
            var imgsList = _mongodbHelper.GetData();
            var result = new List<ImgesVm>();
            foreach (var item in imgsList)
            {
                var imgInfo = new ImgesVm()
                {
                    guid = item.GetValue(1).ToString(),
                    status = item.GetValue(2).ToBoolean()
                };
                result.Add(imgInfo);
            }
            return Json(result);
        }
        [HttpPost]
        public void ExportFile()
        {
            var files = Request.Form.Files;
            var webPath = _hostingEnvironment.WebRootPath;
            foreach (var item in files)
            {
                var fileGuid = Guid.NewGuid().ToString() + ".jpg";
                var bsonDocument = new BsonDocument();
                bsonDocument.Add(new BsonElement("Guid", fileGuid));
                bsonDocument.Add(new BsonElement("Status", true));
                _mongodbHelper.InsetOne(bsonDocument);
                ossClient.PutObject(_bucketName, fileGuid, item.OpenReadStream());
                ossClient.SetObjectAcl(_bucketName, fileGuid, CannedAccessControlList.PublicRead);
            }
        }

        [HttpGet]
        public FileResult GetImg(string guid)
        {
            var response = ossClient.GetObject(_bucketName, guid);
            using (var responseStream = response.Content)
            {

                using (var memeryStrem = new MemoryStream())
                {
                    responseStream.CopyTo(memeryStrem);
                    var fileLen = (int)responseStream.Length;
                    var fileBytes = new byte[fileLen];
                    memeryStrem.Position=0;
                    var readlll= memeryStrem.Read(fileBytes, 0, fileLen);
                    return new FileContentResult(fileBytes, "image/jpeg");
                }
            }
        }

        [HttpPost]
        public JsonResult Update([FromBody]UpdateVm entity)
        {
            var result = _mongodbHelper.UpdateOne(entity.guid, entity.status);
            return Json(result ? "成功" : "失败");
        }
    }
}
