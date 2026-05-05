using Microsoft.AspNetCore.Mvc;
using KlimaKontrolloerenBackend.Models;
using KlimaKontrolloerenBackend.Services;

namespace KlimaKontrolloerenBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SensorController : ControllerBase
{
    private readonly ISensorService _sensorService;
    private readonly IFirebaseService _firebaseService;

    public SensorController(ISensorService sensorService, IFirebaseService firebaseService)
    {
        _sensorService = sensorService;
        _firebaseService = firebaseService;
    }

    private async Task<string?> GetFirebaseUidAsync()
    {
        var authHeader = Request.Headers["Authorization"].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer "))
            return null;

        var token = authHeader["Bearer ".Length..].Trim();
        return await _firebaseService.VerifyTokenAsync(token);
    }

    // POST api/sensor  ← Raspberry Pi sends readings here
    [HttpPost]
    public async Task<IActionResult> PostReading([FromBody] SensorReadingDto dto)
    {
        var uid = await GetFirebaseUidAsync();
        if (uid is null)
            return Unauthorized(new { error = "Invalid or missing Firebase token." });

        if (string.IsNullOrWhiteSpace(dto.SensorId))
            return BadRequest(new { error = "SensorId is required." });

        await _sensorService.SaveReadingAsync(uid, dto);
        return Created(string.Empty, new { message = "Reading saved." });
    }

    // GET api/sensor?limit=100  ← Frontend fetches readings here
    [HttpGet]
    public async Task<IActionResult> GetReadings([FromQuery] int limit = 100)
    {
        var uid = await GetFirebaseUidAsync();
        if (uid is null)
            return Unauthorized(new { error = "Invalid or missing Firebase token." });

        var readings = await _sensorService.GetReadingsAsync(uid, limit);
        return Ok(readings);
    }
}