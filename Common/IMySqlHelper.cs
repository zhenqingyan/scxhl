using System.Collections.Generic;
using System.Threading.Tasks;
using henglong.Web.Models;

namespace henglong.Web.Common
{
    public interface IMySqlHelper
    {
        bool InsertOne(ImgesVm entity);
        Task<IList<ImgesVm>> GetImagesDataAsync();
        bool UpdateStatus(string guid, bool status);
        Task<bool> UpdateLevelAsync(UpdateLevelVm param);
        Task<bool> UpdateSizeAsync(UpdateSizeVm param);
        Task<bool> DeleteOneAsync(string guid);
        Task<int> BulkInsertAsync(IList<ImgesVm> entities);
        void EnsureTableCreated();
    }
}
