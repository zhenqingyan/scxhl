namespace henglong.Web.Models
{
    public class UpdateSizeVm
    {
        public string guid { get; set; } = string.Empty;

        public int width { get; set; }

        public int height { get; set; }

        public decimal percent { get; set; }
    }
}