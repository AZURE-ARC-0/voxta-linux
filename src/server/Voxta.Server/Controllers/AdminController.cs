using Microsoft.AspNetCore.Mvc;

namespace Voxta.Server.Controllers;

[ApiController]
public class AdminController : Controller
{
    private readonly IHostApplicationLifetime _appLifetime;

    public AdminController(IHostApplicationLifetime appLifetime)
    {
        _appLifetime = appLifetime;
    }
    
    [HttpPost("/admin/shutdown")]
    public IActionResult Shutdown()
    {
        _appLifetime.StopApplication();
        return Ok("Shutting down...");
    }
}