using Microsoft.AspNetCore.Mvc;

namespace GrillMaster.Controllers
{
    public class AboutController : Controller
    {
        // GET: /About
        public IActionResult Index()
        {
            return View();
        }
    }
}
