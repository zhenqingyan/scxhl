using henglong.Web.Models;

namespace henglong.Web.Common
{
    public interface IMySqlHelper
    {
        bool InsertOne(ImagesVm entity);
        Task<IList<ImagesVm>> GetImagesDataAsync(int offset, int limit, bool isFilterInvalid, string sortField = "", string sortOrder = "", CancellationToken ct = default);
        Task<int> GetTotalCountImagesDataAsync(CancellationToken ct = default);
        Task<bool> ImageHashExistsAsync(string imageHash, CancellationToken ct = default);
        bool UpdateStatus(string guid, bool status);
        Task<bool> UpdateLevelAsync(UpdateLevelVm param, CancellationToken ct = default);
        Task<bool> UpdateSizeAsync(UpdateSizeVm param, CancellationToken ct = default);
        Task<bool> DeleteOneAsync(string guid, CancellationToken ct = default);
        Task<IReadOnlyList<DuplicateImageGroupVm>> GetDuplicateImageGroupsAsync(CancellationToken ct = default);
        Task<int> CleanDuplicateImagesAsync(CancellationToken ct = default);
    }
}
