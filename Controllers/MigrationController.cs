using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using henglong.Web.Common;

namespace henglong.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MigrationController : ControllerBase
    {
        private readonly IMongoDbHelper<BsonDocument> _mongoHelper;
        private readonly IMySqlHelper _mySqlHelper;

        public MigrationController(IMongoDbHelper<BsonDocument> mongoHelper, IMySqlHelper mySqlHelper)
        {
            _mongoHelper = mongoHelper;
            _mySqlHelper = mySqlHelper;
        }

        [HttpPost("execute")]
        public async Task<IActionResult> MigrateToMySql()
        {
            try
            {
                _mySqlHelper.EnsureTableCreated();

                var mongoData = await _mongoHelper.GetImagesDataAsync();
                if (mongoData == null || mongoData.Count == 0)
                {
                    return Ok(new
                    {
                        success = true,
                        totalInMongoDB = 0,
                        insertedToMySQL = 0,
                        skippedDuplicates = 0,
                        message = "MongoDB 中没有数据需要迁移"
                    });
                }

                var totalCount = mongoData.Count;
                var insertedCount = await _mySqlHelper.BulkInsertAsync(mongoData);
                var skippedCount = totalCount - insertedCount;

                return Ok(new
                {
                    success = true,
                    totalInMongoDB = totalCount,
                    insertedToMySQL = insertedCount,
                    skippedDuplicates = skippedCount,
                    message = "迁移完成"
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    success = false,
                    totalInMongoDB = 0,
                    insertedToMySQL = 0,
                    skippedDuplicates = 0,
                    message = "迁移失败: " + ex.Message
                });
            }
        }
    }
}
