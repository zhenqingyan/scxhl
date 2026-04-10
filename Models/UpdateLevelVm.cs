namespace henglong.Web.Models
{
    public class UpdateLevelVm
    {
        public string guid { get; set; } = string.Empty;

        public int level { get; set; }

        public string number { get; set; } = string.Empty;

        public string composition { get; set; } = string.Empty;
        
        public string yarnCount { get; set; } = string.Empty;

        public string density { get; set; } = string.Empty;

        public string gramWeight { get; set; } = string.Empty;

        public string doorframe { get; set; } = string.Empty;

        public int width { get; set; }

        public int height { get; set; }

        public decimal percent { get; set; }
    }
}
