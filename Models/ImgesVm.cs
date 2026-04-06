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
        /// <summary>
        /// 编号
        /// </summary>
        public string Number { get; set; }
        /// <summary>
        /// 成份
        /// </summary>
        public string Composition { get; set; }
        /// <summary>
        /// 纱支
        /// </summary>
        public string YarnCount { get; set; }
        /// <summary>
        /// 密度
        /// </summary>
        public string Density { get; set; }
        /// <summary>
        /// 克重
        /// </summary>
        public string GramWeight { get; set; }
        /// <summary>
        /// 门幅
        /// </summary>
        public string Doorframe { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public decimal Percent { get; set; }
    }
}