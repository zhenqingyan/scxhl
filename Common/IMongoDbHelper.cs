using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Threading.Tasks;
using henglong.Web.Models;

namespace henglong.Web.Common
{
    public interface IMongoDbHelper<T>
    {
        bool InsetOne(T entity);
        IList<T> GetData();

        Task<IList<ImgesVm>> GetImagesDataAsync();

        IList<T> GetDataByFilter(FilterDefinition<T> filter);
        bool UpdateOne(string key, bool status);

        Task<bool> UpdateLevelAsync(string guid, int level);
        Task<bool> DelOneAsync(string guid);
    }
}