using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;
using henglong.Web.Models;
using System;
namespace henglong.Web.Common
{
    public class MongoDbHelper : IMongoDbHelper<BsonDocument>
    {
        private MongoClient client;
        private IMongoDatabase datebase;
        private IMongoCollection<BsonDocument> collection;
        public MongoDbHelper()
        {
            client = new MongoClient("mongodb://localhost:27017");
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
            var result=new List<ImgesVm>();
            foreach (var item in bsonDocuments.ToList())
            {
                BsonValue guid;
                BsonValue status;
                BsonValue createTime;
                BsonValue name;
                BsonValue level;

                if (!item.TryGetValue("Guid", out guid)) guid = string.Empty;
                if (!item.TryGetValue("Status", out status)) status = true;
                if (!item.TryGetValue("CreateTime", out createTime)) createTime = DateTime.Now;
                if (!item.TryGetValue("Name", out name)) name = string.Empty;
                if (!item.TryGetValue("Level", out level)) level = 1;

                var imgInfo = new ImgesVm()
                {
                    Guid = guid.ToString(),
                    Status = status.ToBoolean(),
                    CreateTime = createTime.ToLocalTime(),
                    Name = name.ToString(),
                    Level = level.ToInt32()
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
        public async Task<bool> UpdateLevelAsync(string guid, int level)
        {
            try
            {
                FilterDefinition<BsonDocument> filter = Builders<BsonDocument>.Filter.Eq("Guid", guid);
                UpdateDefinition<BsonDocument> update = Builders<BsonDocument>.Update.Set("Level", level);
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