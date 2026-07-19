using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using henglong.Web.Models;
using henglong.Web.Common;
using Aliyun.OSS;
using SixLabors.ImageSharp;
using System.Security.Cryptography;

namespace henglong.Web.Controllers
{
    [Authorize]
    public class ProductController : Controller
    {
        private readonly IMySqlHelper _mySqlHelper;
        private readonly OssClient _ossClient;
        private readonly string _bucketName;
        private readonly ILogger<ProductController> _logger;

        // Allowed image extensions for upload
        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp"
        };

        // Max file size: 20 MB
        private const long MaxFileSize = 20 * 1024 * 1024;

        public ProductController(IMySqlHelper mySqlHelper,
            IConfiguration configuration,
            ILogger<ProductController> logger)
        {
            _mySqlHelper = mySqlHelper;
            _logger = logger;

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
        public async Task<IActionResult> GetImgs([FromBody] QueryVm entity, CancellationToken ct)
        {
            // Validate pagination params
            if (entity.current < 1) entity.current = 1;
            if (entity.pageSize < 1) entity.pageSize = 12;
            if (entity.pageSize > 100) entity.pageSize = 100;

            var offset = (entity.current - 1) * entity.pageSize;
            var imgsList = await _mySqlHelper.GetImagesDataAsync(offset, entity.pageSize, false, entity.sortField, entity.sortOrder, ct);
            var totalCount = await _mySqlHelper.GetTotalCountImagesDataAsync(ct);
            return Json(new
            {
                data = imgsList,
                total = totalCount
            });
        }

        [HttpPost]
        [RequestSizeLimit(MaxFileSize)]
        public async Task<IActionResult> ExportFile(CancellationToken ct)
        {
            var files = Request.Form.Files;
            if (files.Count == 0)
            {
                return Json(new { success = false, message = "未选择文件" });
            }

            var uploaded = 0;
            var duplicate = 0;
            var invalidType = 0;
            var oversized = 0;
            var identifyFailed = 0;

            foreach (var item in files)
            {
                // Validate file extension
                var ext = Path.GetExtension(item.FileName);
                if (!AllowedExtensions.Contains(ext))
                {
                    _logger.LogWarning("Rejected file with unsupported extension: {FileName}", item.FileName);
                    invalidType++;
                    continue;
                }

                // Validate file size
                if (item.Length > MaxFileSize)
                {
                    _logger.LogWarning("Rejected oversized file: {FileName} ({Size} bytes)", item.FileName, item.Length);
                    oversized++;
                    continue;
                }

                using var ms = new MemoryStream();
                await item.CopyToAsync(ms, ct);
                ms.Position = 0;
                var imageHash = await ComputeSha256Async(ms, ct);
                if (await _mySqlHelper.ImageHashExistsAsync(imageHash, ct))
                {
                    _logger.LogInformation("Skipped duplicate image upload: {FileName} ({ImageHash})", item.FileName, imageHash);
                    duplicate++;
                    continue;
                }

                ms.Position = 0;

                ImageInfo? imageInfo;
                try
                {
                    imageInfo = Image.Identify(ms) as ImageInfo;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to identify image format: {FileName}", item.FileName);
                    identifyFailed++;
                    continue;
                }

                if (imageInfo == null)
                {
                    identifyFailed++;
                    continue;
                }

                int width = imageInfo.Width;
                int height = imageInfo.Height;
                decimal percent = width > 0 ? Math.Round((height * 1M / (width * 1M)), 2) : 0;

                var fileGuid = Guid.NewGuid().ToString() + ".jpg";
                var entity = new ImagesVm
                {
                    Guid = fileGuid,
                    Status = true,
                    CreateTime = DateTime.Now,
                    Name = item.FileName,
                    ImageHash = imageHash,
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
                uploaded++;
            }

            var messageParts = new List<string> { $"成功上传 {uploaded} 个文件" };
            if (duplicate > 0) messageParts.Add($"跳过 {duplicate} 个重复文件");
            if (invalidType > 0) messageParts.Add($"跳过 {invalidType} 个格式不支持文件");
            if (oversized > 0) messageParts.Add($"跳过 {oversized} 个超大文件");
            if (identifyFailed > 0) messageParts.Add($"跳过 {identifyFailed} 个无法识别图片");

            return Json(new
            {
                success = true,
                uploaded,
                duplicate,
                invalidType,
                oversized,
                identifyFailed,
                skipped = duplicate + invalidType + oversized + identifyFailed,
                message = string.Join("，", messageParts)
            });
        }

        [HttpGet]
        public async Task<FileResult> GetImg(string guid, CancellationToken ct)
        {
            var response = _ossClient.GetObject(_bucketName, guid);
            using (var responseStream = response.Content)
            {
                using (var memeryStrem = new MemoryStream())
                {
                    await responseStream.CopyToAsync(memeryStrem, ct);
                    var fileLen = (int)responseStream.Length;
                    var fileBytes = new byte[fileLen];
                    memeryStrem.Position = 0;
                    var readlll = await memeryStrem.ReadAsync(fileBytes, 0, fileLen, ct);
                    return new FileContentResult(fileBytes, "image/jpeg");
                }
            }
        }

        [HttpPost]
        public JsonResult Update([FromBody] UpdateVm entity)
        {
            if (string.IsNullOrWhiteSpace(entity.guid))
            {
                return Json("失败：无效的产品标识");
            }

            var result = _mySqlHelper.UpdateStatus(entity.guid, entity.status);
            return Json(result ? "成功" : "失败");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateLevel([FromBody] UpdateLevelVm entity, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(entity.guid))
            {
                return Json("失败：无效的产品标识");
            }

            entity.percent = Math.Round((entity.height * 1M / (entity.width * 1M)), 2);
            var result = await _mySqlHelper.UpdateLevelAsync(entity, ct);
            return Json(result ? "成功" : "失败");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateSize([FromBody] UpdateSizeVm entity, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(entity.guid))
            {
                return Json("失败：无效的产品标识");
            }

            entity.percent = Math.Round((entity.height * 1M / (entity.width * 1M)), 2);
            var result = await _mySqlHelper.UpdateSizeAsync(entity, ct);
            return Json(result ? "成功" : "失败");
        }

        [HttpPost]
        public async Task<IActionResult> Del([FromBody] DelVm entity, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(entity.guid))
            {
                return Json(0);
            }

            var result = await _mySqlHelper.DeleteOneAsync(entity.guid, ct);
            return Json(result ? 1 : 0);
        }

        [HttpPost]
        public async Task<IActionResult> GetDuplicateData(CancellationToken ct)
        {
            var groups = await _mySqlHelper.GetDuplicateImageGroupsAsync(ct);
            var deleteCount = groups.Sum(group => group.DeleteItems.Count);

            return Json(new
            {
                success = true,
                groups,
                groupCount = groups.Count,
                deleteCount
            });
        }

        [HttpPost]
        public async Task<IActionResult> CleanDuplicateData(CancellationToken ct)
        {
            var deletedCount = await _mySqlHelper.CleanDuplicateImagesAsync(ct);
            if (deletedCount < 0)
            {
                return Json(new { success = false, message = "清理失败" });
            }

            return Json(new
            {
                success = true,
                deletedCount,
                message = deletedCount > 0 ? $"成功清理 {deletedCount} 条重复数据" : "未发现重复数据"
            });
        }

        private static async Task<string> ComputeSha256Async(Stream stream, CancellationToken ct)
        {
            using var sha256 = SHA256.Create();
            var hash = await sha256.ComputeHashAsync(stream, ct);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }
    }
}
