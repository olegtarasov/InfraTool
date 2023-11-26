using System.Collections.Concurrent;
using Common.Host.Web.Api;
using Microsoft.AspNetCore.Mvc;

namespace UpdateWatcher.Controllers;

public record Unit(string Name, Version Local, Version Remote);

[Route("/api/updates")]
public class UpdatesController : ApiControllerBase
{
    private readonly ILogger<UpdatesController> _logger;

    public UpdatesController(ILogger<UpdatesController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    [Route("")]
    public async Task<Unit[]> GetVersions()
    {
        var config = WatcherConfig.Load();
        var bag = new ConcurrentBag<Unit>();

        await Parallel.ForEachAsync(config.Items, async (item, _) =>
        {
            Version? local = null, remote = null;

            try
            {
                local = await item.Local.GetVersion();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to get local version for item '{Item}'", item.Name);
            }

            try
            {
                remote = await item.Remote.GetVersion();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to get remote version for item '{Item}'", item.Name);
            }
            
            bag.Add(new(item.Name, local ?? new(), remote ?? new()));
        });

        return bag.ToArray();
    }
}