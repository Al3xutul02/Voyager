using Microsoft.AspNetCore.Mvc;

namespace Voyager.API.Controllers;

/// <summary>
/// Controller for testing connection to the WebAPI.
/// </summary>
[ApiController]
[Route("[controller]")]
public class TestController : ControllerBase
{
    [HttpGet("Ping")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult Ping() => Ok("Pong");
}