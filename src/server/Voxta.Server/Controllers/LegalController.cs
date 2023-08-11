using Microsoft.AspNetCore.Mvc;

namespace Voxta.Server.Controllers;

[Controller]
public class LegalController : Controller
{
    [HttpGet("/legal/tos")]
    public ActionResult TermsOfService()
    {
        return View();
    }
}