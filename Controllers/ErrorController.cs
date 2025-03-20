using Microsoft.AspNetCore.Mvc;

namespace STIN_project_2025.Controllers
{
    public class ErrorController : Controller
    {
        [Route("Error/{code}")]
        public IActionResult HandleErrorCode(int code)
        {
            if (code == 404)
            {
                // Vracíme view z umístění Pages/Error/NotFound.cshtml
                return View("~/Pages/Error/NotFound.cshtml");
            }
            // Pro ostatní kódy můžete vytvořit obecné zpracování chyby
            return View("Error");
        }
    }
}