using Microsoft.AspNetCore.Mvc;

namespace Voyager.API.Controllers;

/// <summary>
/// Controller for testing connection to the WebAPI.
/// </summary>
[ApiController]
[Route("[controller]")]
public class TestController : ControllerBase
{
    /// <summary>
    /// Liveness probe. Returns 200 OK with body <c>"Pong"</c> when the API
    /// is responding. Use this from monitoring/Docker health checks.
    /// </summary>
    [HttpGet("Ping")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult Ping() => Ok("Pong");
}