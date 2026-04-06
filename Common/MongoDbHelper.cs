using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;
using henglong.Web.Models;
using System;
using System.Reflection.PortableExecutable;

namespace henglong.Web.Common
{
    public class MongoDbHelper : IMongoDbHelper<BsonDocument>
    {
        private MongoClient client;
        private IMongoDatabase datebase;
        private IMongoCollection<BsonDocument> collection;
        public MongoDbHelper()
        {
#if DEBUG
            client = new MongoClient("mongodb://localhost");
#else
            client = new MongoClient("mongodb://localhost:27017");
#endif

            datebase = client.GetDatabase("henglong");
            collection = datebase.GetCollection<BsonDocument>("henglong");
        }
        public bool InsetOne(BsonDocument entity)
        {
            try
            {
                collection.InsertOne(entity);
                return true;
            }
            catch (System.Exception)
            {
                return false;
            }

        }

        public IList<BsonDocument> GetData()
        {
            return collection.Find(new BsonDocument()).ToList();
        }

        public async Task<IList<ImgesVm>> GetImagesDataAsync()
        {
            var bsonDocuments = await collection.FindAsync(new BsonDocument());
            var result = new List<ImgesVm>();
            foreach (var item in bsonDocuments.ToList())
            {
                BsonValue guid;
                BsonValue status;
                BsonValue createTime;
                BsonValue name;
                BsonValue level;
                BsonValue number;
                BsonValue composition;
                BsonValue yarnCount;
                BsonValue density;
                BsonValue gramWeight;
                BsonValue doorframe;
                BsonValue width;
                BsonValue height;
                BsonValue percent;

                if (!item.TryGetValue("Guid", out guid)) guid = string.Empty;
                if (!item.TryGetValue("Status", out status)) status = true;
                if (!item.TryGetValue("CreateTime", out createTime)) createTime = DateTime.Now;
                if (!item.TryGetValue("Name", out name)) name = string.Empty;
                if (!item.TryGetValue("Level", out level)) level = 1;
                if (!item.TryGetValue("Number", out number)) number = string.Empty;
                if (!item.TryGetValue("Composition", out composition)) composition = string.Empty;
                if (!item.TryGetValue("YarnCount", out yarnCount)) yarnCount = string.Empty;
                if (!item.TryGetValue("Density", out density)) density = string.Empty;
                if (!item.TryGetValue("GramWeight", out gramWeight)) gramWeight = string.Empty;
                if (!item.TryGetValue("Doorframe", out doorframe)) doorframe = string.Empty;
                if (!item.TryGetValue("Width", out width)) width = 0;
                if (!item.TryGetValue("Height", out height)) height = 0;
                if (!item.TryGetValue("Percent", out percent)) percent = 0M;

                var imgInfo = new ImgesVm()
                {
                    Guid = guid.ToString(),
                    Status = status.ToBoolean(),
                    CreateTime = createTime.ToLocalTime(),
                    Name = name.ToString(),
                    Level = level.ToInt32(),
                    Number = number.ToString(),
                    Composition = composition.ToString(),
                    YarnCount = yarnCount.ToString(),
                    Density = density.ToString(),
                    GramWeight = gramWeight.ToString(),
                    Doorframe = doorframe.ToString(),
                    Width = width.ToInt32(),
                    Height = height.ToInt32(),
                    Percent = percent.ToDecimal()
                };
                result.Add(imgInfo);
            }
            return result;
        }

        public IList<BsonDocument> GetDataByFilter(FilterDefinition<BsonDocument> filter)
        {
            return collection.Find(filter).ToList();
        }

        public bool UpdateOne(string key, bool status)
        {
            try
            {
                FilterDefinition<BsonDocument> filter = Builders<BsonDocument>.Filter.Eq("Guid", key);
                UpdateDefinition<BsonDocument> update = Builders<BsonDocument>.Update.Set("Status", status);
                UpdateResult result = collection.UpdateOne(filter, update);
                return true;
            }
            catch (System.Exception)
            {
                return false;
            }
        }

        public async Task<bool> DelOneAsync(string guid)
        {
            try
            {
                FilterDefinition<BsonDocument> filter = Builders<BsonDocument>.Filter.Eq("Guid", guid);
                var delResult = await collection.DeleteOneAsync(filter);
                return delResult.DeletedCount == 1;
            }
            catch (System.Exception)
            {
                return false;
            }

        }
        public async Task<bool> UpdateLevelAsync(UpdateLevelVm param)
        {
            try
            {
                FilterDefinition<BsonDocument> filter = Builders<BsonDocument>.Filter.Eq("Guid", param.guid);
                UpdateDefinition<BsonDocument> update = Builders<BsonDocument>.Update.Set("Level", param.level)
                    .Set("Number", param.number)
                    .Set("Composition", param.composition)
                    .Set("YarnCount", param.yarnCount)
                    .Set("Density", param.density)
                    .Set("GramWeight", param.gramWeight)
                    .Set("Doorframe", param.doorframe)
                    .Set("Width", param.width)
                    .Set("Height", param.height)
                    .Set("Percent", param.percent);
                UpdateResult result = await collection.UpdateOneAsync(filter, update);
                return result.ModifiedCount == 1;
            }
            catch (System.Exception)
            {
                return false;
            }
        }

        public async Task<bool> UpdateSizeAsync(UpdateSizeVm param)
        {
            try
            {
                FilterDefinition<BsonDocument> filter = Builders<BsonDocument>.Filter.Eq("Guid", param.guid);
                UpdateDefinition<BsonDocument> update = Builders<BsonDocument>.Update.Set("Width", param.width)
                    .Set("Height", param.height)
                    .Set("Percent", param.percent);
                UpdateResult result = await collection.UpdateOneAsync(filter, update);
                return result.ModifiedCount == 1;
            }
            catch (System.Exception)
            {
                return false;
            }
        }
    }
}