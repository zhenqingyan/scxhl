using henglong.Web.Models;

namespace henglong.Web.Common
{
    public interface IMySqlHelper
    {
        bool InsertOne(ImagesVm entity);
        Task<IList<ImagesVm>> GetImagesDataAsync(int offset, int limit, bool isFilterInvalid, CancellationToken ct = default);
        Task<int> GetTotalCountImagesDataAsync(CancellationToken ct = default);
        bool UpdateStatus(string guid, bool status);
        Task<bool> UpdateLevelAsync(UpdateLevelVm param, CancellationToken ct = default);
        Task<bool> UpdateSizeAsync(UpdateSizeVm param, CancellationToken ct = default);
        Task<bool> DeleteOneAsync(string guid, CancellationToken ct = default);
    }
}
