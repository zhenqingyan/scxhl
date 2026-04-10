using Microsoft.AspNetCore.Mvc;

namespace henglong.Web.Controllers
{
    public class HomeController:Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}