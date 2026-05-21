using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using KlimaKontrolloerenBackend.Models;
using KlimaKontrolloerenBackend.Services;

namespace KlimaKontrolloerenBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [EnableRateLimiting("auth-signin")]
    [HttpPost("signin")]
    public async Task<IActionResult> SignIn([FromBody] SignInRequest request)
    {
        if (request is null || string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
            return BadRequest(new { error = "Email and password are required." });

        var result = await _authService.SignInAsync(request.Email, request.Password);
        if (result == null)
            return Unauthorized(new { error = "Invalid email or password or user is disabled." });

        return Ok(result);
    }

    [EnableRateLimiting("auth-token")]
    [HttpGet("getUserUID")]
    public async Task<IActionResult> GetUserUID([FromQuery] string token)
    {
        var uid = await _authService.GetUserUIDAsync(token);
        if (uid == null)
            return NotFound(new { error = "UID could not be resolved from the token." });

        return Ok(new { uid });
    }
}
