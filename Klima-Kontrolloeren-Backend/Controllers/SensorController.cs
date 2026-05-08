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
}