using System;

namespace henglong.Web.Models
{
    public class ImgesVm
    {
        public int Id { get; set; }
        public string Guid { get; set; }
        public bool Status { get; set; }
        public DateTime CreateTime { get; set; }
        public string Name { get; set; }
        public int Level { get; set; }
    }
}