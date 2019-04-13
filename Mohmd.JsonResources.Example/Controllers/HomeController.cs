using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Mohmd.JsonResources.Example.Models;

namespace Mohmd.JsonResources.Example.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public IActionResult ChangeLanguage(string lang, string returnUrl)
        {
            if (!string.IsNullOrEmpty(lang))
            {
                HttpContext.Response.Cookies.Append(
                    key: CookieRequestCultureProvider.DefaultCookieName,
                    value: CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(lang)));
            }

            if (string.IsNullOrWhiteSpace(returnUrl))
            {
                return RedirectToAction(nameof(Index));
            }
            else
            {
                return LocalRedirect(returnUrl);
            }
        }
    }
}
