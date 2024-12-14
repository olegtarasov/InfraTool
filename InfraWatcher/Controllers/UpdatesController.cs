using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;

namespace InfraWatcher.Controllers;

[Route("/api/updates")]
[ApiController]
[Produces(MediaTypeNames.Application.Json)]
[Consumes(MediaTypeNames.Application.Json)]
[ProducesResponseType(StatusCodes.Status200OK)]
public class UpdatesController : ControllerBase
{
    private readonly VersionWatcher _versionWatcher;

    public UpdatesController(VersionWatcher versionWatcher)
    {
        _versionWatcher = versionWatcher;
    }

    [HttpGet]
    [Route("")]
    public Task<Unit[]> GetVersions()
    {
        return _versionWatcher.GetVersions();
    }
}