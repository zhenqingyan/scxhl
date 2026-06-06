using henglong.Web.Models;

namespace henglong.Web.Common
{
    public interface IMySqlHelper
    {
        bool InsertOne(ImgesVm entity);
        Task<IList<ImgesVm>> GetImagesDataAsync(int startIndex,int endIndex,bool isFilterInvalid);
        Task<int> GetTotalCountImagesDataAsync();
        bool UpdateStatus(string guid, bool status);
        Task<bool> UpdateLevelAsync(UpdateLevelVm param);
        Task<bool> UpdateSizeAsync(UpdateSizeVm param);
        Task<bool> DeleteOneAsync(string guid);
    }
}
