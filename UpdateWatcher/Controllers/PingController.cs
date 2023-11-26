using Common.Host.Web.Api;
using Microsoft.AspNetCore.Mvc;

namespace UpdateWatcher.Controllers;

[Route("/api/ping")]
public class PingController : ApiControllerBase
{
    [HttpGet]
    [Route("")]
    public IActionResult Ping()
    {
        return Ok();
    }
}