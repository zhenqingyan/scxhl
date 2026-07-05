namespace henglong.Web.Models
{
    public class ImagesVm
    {
        public int Id { get; set; }
        public string Guid { get; set; } = string.Empty;
        public bool Status { get; set; }
        public DateTime CreateTime { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Level { get; set; }
        public string Number { get; set; } = string.Empty;
        public string Composition { get; set; } = string.Empty;
        public string YarnCount { get; set; } = string.Empty;
        public string Density { get; set; } = string.Empty;
        public string GramWeight { get; set; } = string.Empty;
        public string Doorframe { get; set; } = string.Empty;
        public int Width { get; set; }
        public int Height { get; set; }
        public decimal Percent { get; set; }
    }
}
