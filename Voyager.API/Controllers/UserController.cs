using BusinessLogic.Dtos.User;
using BusinessLogic.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Voyager.API.Controllers;

/// <summary>
/// Controller for things related to user information.
/// </summary>
/// <param name="userService">The service used for handling user information.</param>
[ApiController]
[Route("[controller]")]
public class UserController(IUserService userService) : ControllerBase
{
    private readonly IUserService _userService = userService;

    /// <summary>
    /// Endpoint for getting user information.
    /// </summary>
    /// <param name="name">The name of the user.</param>
    /// <returns>The user DTO.</returns>
    [HttpGet("get")]
    [ProducesResponseType(typeof(UserReadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Exists(string name)
    {
        try
        {
            var user = await _userService.GetByName(name);
            return Ok(user);
        }
        catch(Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
