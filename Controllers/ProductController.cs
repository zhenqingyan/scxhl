using Microsoft.AspNetCore.Mvc;
using henglong.Web.Models;
using henglong.Web.Common;
using Aliyun.OSS;
using SixLabors.ImageSharp;

namespace henglong.Web.Controllers
{
    public class ProductController : Controller
    {
        private readonly IMySqlHelper _mySqlHelper;
        private readonly OssClient _ossClient;
        private readonly string _bucketName;

        public ProductController(IMySqlHelper mySqlHelper,
            IConfiguration configuration)
        {
            _mySqlHelper = mySqlHelper;

            var endPoint = configuration["AliyunOss:EndPoint"] ?? string.Empty;
            var accessKey = configuration["AliyunOss:AccessKey"] ?? string.Empty;
            var accessSecret = configuration["AliyunOss:AccessSecret"] ?? string.Empty;
            _bucketName = configuration["AliyunOss:BucketName"] ?? string.Empty;

            _ossClient = new OssClient(endPoint, accessKey, accessSecret);
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GetImgs([FromBody] QueryVm entity)
        {
            var imgsList =
                await _mySqlHelper.GetImagesDataAsync((entity.current - 1) * entity.pageSize, entity.pageSize,false);
            var totalCount = await _mySqlHelper.GetTotalCountImagesDataAsync();
            return Json(new
            {
                data = imgsList,
                total = totalCount
            });
        }

        [HttpPost]
        public void ExportFile()
        {
            var files = Request.Form.Files;
            foreach (var item in files)
            {
                using var ms = new MemoryStream();
                item.CopyTo(ms);
                ms.Position = 0;

                ImageInfo? imageInfo;
                try
                {
                    imageInfo = Image.Identify(ms) as ImageInfo;
                }
                catch
                {
                    continue;
                }

                if (imageInfo == null)
                    continue;

                int width = imageInfo.Width;
                int height = imageInfo.Height;
                decimal percent = width > 0 ? Math.Round((height * 1M / (width * 1M)), 2) : 0;

                var fileGuid = Guid.NewGuid().ToString() + ".jpg";
                var entity = new ImgesVm
                {
                    Guid = fileGuid,
                    Status = true,
                    CreateTime = DateTime.Now,
                    Name = item.FileName,
                    Level = 1,
                    Number = "",
                    Composition = "",
                    YarnCount = "",
                    Density = "",
                    GramWeight = "",
                    Doorframe = "",
                    Width = width,
                    Height = height,
                    Percent = percent
                };
                _mySqlHelper.InsertOne(entity);

                ms.Position = 0;
                _ossClient.PutObject(_bucketName, fileGuid, ms);
                _ossClient.SetObjectAcl(_bucketName, fileGuid, CannedAccessControlList.PublicRead);
            }
        }

        [HttpGet]
        public async Task<FileResult> GetImg(string guid)
        {
            var response = _ossClient.GetObject(_bucketName, guid);
            using (var responseStream = response.Content)
            {
                using (var memeryStrem = new MemoryStream())
                {
                    await responseStream.CopyToAsync(memeryStrem);
                    var fileLen = (int)responseStream.Length;
                    var fileBytes = new byte[fileLen];
                    memeryStrem.Position = 0;
                    var readlll = await memeryStrem.ReadAsync(fileBytes, 0, fileLen);
                    return new FileContentResult(fileBytes, "image/jpeg");
                }
            }
        }

        [HttpPost]
        public JsonResult Update([FromBody] UpdateVm entity)
        {
            var result = _mySqlHelper.UpdateStatus(entity.guid, entity.status);
            return Json(result ? "成功" : "失败");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateLevel([FromBody] UpdateLevelVm entity)
        {
            entity.percent = Math.Round((entity.height * 1M / (entity.width * 1M)), 2);
            var result = await _mySqlHelper.UpdateLevelAsync(entity);
            return Json(result ? "成功" : "失败");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateSize([FromBody] UpdateSizeVm entity)
        {
            entity.percent = Math.Round((entity.height * 1M / (entity.width * 1M)), 2);
            var result = await _mySqlHelper.UpdateSizeAsync(entity);
            return Json(result ? "成功" : "失败");
        }

        [HttpPost]
        public async Task<IActionResult> Del([FromBody] DelVm entity)
        {
            var result = await _mySqlHelper.DeleteOneAsync(entity.guid);
            return Json(result ? 1 : 0);
        }
    }
}