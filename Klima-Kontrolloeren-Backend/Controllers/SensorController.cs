using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
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
    [EnableRateLimiting("sensor-ingest")]
    [HttpPost]
    public async Task<IActionResult> PostReading([FromBody] SensorReadingDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.SensorId))
            return BadRequest(new { error = "SensorId is required." });

        await _sensorService.SaveReadingAsync(dto);
        return Created(string.Empty, new { message = "Reading saved." });
    }

    [EnableRateLimiting("sensor-read")]
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

    // GET api/sensor/info?uid=X&sensorId=Y
    [EnableRateLimiting("sensor-read")]
    [HttpGet("info")]
    public async Task<IActionResult> GetSensorInfo([FromQuery] string uid, [FromQuery] string sensorId)
    {
        if (string.IsNullOrWhiteSpace(uid) || string.IsNullOrWhiteSpace(sensorId))
            return BadRequest(new { error = "uid and sensorId are required." });
        var info = await _sensorService.GetSensorInfoAsync(uid, sensorId);
        return Ok(info ?? new SensorInfoResult { SensorId = sensorId, Name = sensorId });
    }

    // PUT api/sensor/info  ← save name / type / location
    [EnableRateLimiting("sensor-read")]
    [HttpPut("info")]
    public async Task<IActionResult> UpdateSensorInfo([FromBody] UpdateSensorInfoRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Uid) || string.IsNullOrWhiteSpace(req.SensorId))
            return BadRequest(new { error = "uid and sensorId are required." });
        await _sensorService.UpdateSensorInfoAsync(req.Uid, req.SensorId, req.Name, req.Type, req.Location);
        return Ok(new { message = "Sensor info saved." });
    }

    // POST api/sensor/add  ← add a sensor ID to the user's profile
    [EnableRateLimiting("sensor-read")]
    [HttpPost("add")]
    public async Task<IActionResult> AddSensor([FromBody] AddSensorRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Uid) || string.IsNullOrWhiteSpace(req.SensorId))
            return BadRequest(new { error = "uid and sensorId are required." });
        await _sensorService.AddSensorToUserAsync(req.Uid, req.SensorId);
        return Ok(new { message = $"Sensor '{req.SensorId}' added." });
    }

    // DELETE api/sensor/remove?uid=X&sensorId=Y  ← remove a sensor from the user's profile
    [EnableRateLimiting("sensor-read")]
    [HttpDelete("remove")]
    public async Task<IActionResult> RemoveSensor([FromQuery] string uid, [FromQuery] string sensorId)
    {
        if (string.IsNullOrWhiteSpace(uid) || string.IsNullOrWhiteSpace(sensorId))
            return BadRequest(new { error = "uid and sensorId are required." });
        await _sensorService.RemoveSensorFromUserAsync(uid, sensorId);
        return Ok(new { message = $"Sensor '{sensorId}' removed." });
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
