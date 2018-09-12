using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Driver;

namespace henglong.Web.Common
{
    public interface IMongoDbHelper<T>
    {
         bool InsetOne(T entity);
         IList<T> GetData();
         IList<T> GetDataByFilter(FilterDefinition<T> filter);
         bool UpdateOne(string key,bool status);
    }
}