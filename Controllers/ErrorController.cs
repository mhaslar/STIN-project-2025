using Microsoft.AspNetCore.Mvc;

namespace STIN_project_2025.Controllers
{
    [ApiController]
    [Route("Error")]
    public class ErrorController : Controller
    {
        [HttpGet("{code}")]
        public IActionResult HandleErrorCode(int code)
        {
            if (code == 404)
            {
                // Pokud používáš Razor Pages, cesta musí být správně relativní
                return View("~/Pages/Error/NotFound.cshtml");
            }

            return View("~/Pages/Error/GenericError.cshtml"); // nebo jen "Error" pokud máš Views/Error/Error.cshtml
        }
    }
}
