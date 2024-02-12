using Microsoft.AspNetCore.Mvc;

namespace SampleWebsite.Code.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
