using System.Net;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;

namespace CourseManagement.Controllers.Internal;

[ApiController]
[Route("internal/redis")]
[ApiExplorerSettings(IgnoreApi = true)]
public class RedisInspectorController : ControllerBase
{
    private readonly IConnectionMultiplexer _redis;

    public RedisInspectorController(
        IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    [HttpGet("keys")]
    public IActionResult GetKeys()
    {
        EndPoint endpoint = _redis.GetEndPoints().First();

        IServer server = _redis.GetServer(endpoint);

        IEnumerable<string> keys = server
            .Keys()
            .Select(k => k.ToString())
            .ToList();

        return Ok(keys);
    }
}
