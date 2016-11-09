using FeatureFlags;
using Microsoft.AspNetCore.Mvc;

namespace TestWebApp.Controllers
{
    public class HomeController : Controller
    {
        public IFeatures Features { get; set; }

        public HomeController(IFeatures features)
        {
            Features = features;
        }

        public IActionResult Index()
        {
            if (Features.IsActive("UserFeature"))
                ViewData["Message"] = "UserFeature is activated";
            else
                ViewData["Message"] = "UserFeature is NOT activated";
            
            return View();
        }

        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";

            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}
