namespace henglong.Web.Models
{
    public class QueryVm
    {
       public int current { get; set; }
       public int pageSize { get; set; }
       public string sortField { get; set; } = string.Empty;
       public string sortOrder { get; set; } = string.Empty;
    }
}
