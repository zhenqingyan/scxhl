using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Generic;
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
    }
}