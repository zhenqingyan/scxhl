namespace henglong.Web.Models
{
    public class DuplicateImageGroupVm
    {
        public string Name { get; set; } = string.Empty;
        public string ImageHash { get; set; } = string.Empty;
        public int Width { get; set; }
        public int Height { get; set; }
        public ImagesVm? KeepItem { get; set; }
        public IList<ImagesVm> DeleteItems { get; set; } = new List<ImagesVm>();
    }
}
