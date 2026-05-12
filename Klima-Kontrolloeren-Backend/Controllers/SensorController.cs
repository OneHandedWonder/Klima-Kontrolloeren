using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using KlimaKontrolloerenBackend.Models;
using KlimaKontrolloerenBackend.Services;

namespace KlimaKontrolloerenBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SensorController : ControllerBase
{
    private readonly ISensorService _sensorService;

    public SensorController(ISensorService sensorService)
    {
        _sensorService = sensorService;
    }

    // POST api/sensor  ← Raspberry Pi sends readings here
    [HttpPost]
    public async Task<IActionResult> PostReading([FromBody] SensorReadingDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.SensorId))
            return BadRequest(new { error = "SensorId is required." });

        await _sensorService.SaveReadingAsync(dto);
        return Created(string.Empty, new { message = "Reading saved." });
    }
    [HttpGet]
    public async Task<IActionResult> GetUserSensors([FromQuery] string uid)
    {
        if (string.IsNullOrWhiteSpace(uid))
            return BadRequest(new { error = "UID is required." });

        var users = await _sensorService.GetUserSensorsAsync(uid);
        if (users == null || !users.Any())
            return NotFound(new { error = "No sensors found for this user." });

        var response = users.Select(u => new
        {
            uid = u.Uid,
            sensors = ParseSensorsArray(u.Sensors),
            enabled = u.Enabled
        }).ToList();

        return Ok(response);
    }

    private static string[] ParseSensorsArray(string sensorsString)
    {
        if (string.IsNullOrWhiteSpace(sensorsString))
            return Array.Empty<string>();

        try
        {
            // Try to parse as JSON array first
            var parsed = JsonSerializer.Deserialize<string[]>(sensorsString);
            return parsed ?? Array.Empty<string>();
        }
        catch
        {
            // If it's comma-separated with quotes like: "pi-sensor-01", "pi-sensor-02"
            // Remove outer quotes if present and split by comma
            var cleaned = sensorsString.Trim().Trim('"', '\'');
            if (cleaned.Contains(','))
            {
                return cleaned.Split(',')
                    .Select(s => s.Trim().Trim('"', '\''))
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToArray();
            }
            
            // Single sensor
            var trimmed = sensorsString.Trim('"', '\'').Trim();
            return string.IsNullOrWhiteSpace(trimmed) ? Array.Empty<string>() : new[] { trimmed };
        }
    }
}