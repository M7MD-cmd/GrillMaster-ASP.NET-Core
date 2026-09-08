using Microsoft.AspNetCore.Mvc;

namespace GrillMaster.Controllers
{
    public class BranchesController : Controller
    {
        // GET: /Branches
        public IActionResult Index()
        {
            return View();
        }
    }
}
