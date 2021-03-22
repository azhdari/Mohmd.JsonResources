using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Mohmd.JsonResources.ExampleCore50.Models;
using System.Diagnostics;

namespace Mohmd.JsonResources.ExampleCore50.Controllers {
    public class HomeController : Controller {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger) {
            _logger = logger;
        }

        public IActionResult Index() {
            return View();
        }

        public IActionResult Privacy() {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public IActionResult ChangeLanguage(string lang, string returnUrl) {
            if (!string.IsNullOrEmpty(lang)) {
                HttpContext.Response.Cookies.Append(
                    key: CookieRequestCultureProvider.DefaultCookieName,
                    value: CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(lang)));
            }

            if (string.IsNullOrWhiteSpace(returnUrl)) {
                return RedirectToAction(nameof(Index));
            }
            else {
                return LocalRedirect(returnUrl);
            }
        }
    }
}
