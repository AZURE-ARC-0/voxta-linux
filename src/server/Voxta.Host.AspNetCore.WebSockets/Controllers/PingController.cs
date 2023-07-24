using Microsoft.AspNetCore.Mvc;

namespace Voxta.Host.AspNetCore.WebSockets.Controllers;

[ApiController]
public class PingController : ControllerBase
{
    [HttpGet("/ping")]
    public ActionResult Get()
    {
        return Ok("Voxta Server is running");
    }
}