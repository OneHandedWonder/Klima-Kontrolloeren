using Microsoft.AspNetCore.Mvc;
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

    // GET api/sensor?limit=100  ← Frontend fetches readings here
    [HttpGet]
    public async Task<IActionResult> GetReadings([FromQuery] int limit = 100)
    {
        if (limit <= 0)
            return BadRequest(new { error = "limit must be greater than 0." });

        var readings = await _sensorService.GetReadingsAsync(limit);
        return Ok(readings);
    }
}