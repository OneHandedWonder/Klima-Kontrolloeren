using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using KlimaKontrolloerenBackend.Models;
using KlimaKontrolloerenBackend.Services;

namespace KlimaKontrolloerenBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("data")]
public class DataController : ControllerBase
{
    private readonly ISensorService _sensorService;
    private readonly ICsvService _csvService;

    public DataController(ISensorService sensorService, ICsvService csvService)
    {
        _sensorService = sensorService;
        _csvService = csvService;
    }

    // GET api/sensor?limit=100  ← Frontend fetches readings here
    [HttpGet]
    public async Task<IActionResult> GetReadings([FromQuery] int limit = 80640, [FromQuery] string? uid = null)
    {
        if (limit <= 0)
            return BadRequest(new { error = "limit must be greater than 0." });

        var readings = string.IsNullOrWhiteSpace(uid)
            ? await _sensorService.GetReadingsAsync(limit)
            : await _sensorService.GetReadingsAsync(limit, uid);

        return Ok(readings);
    }

    [HttpGet("export")]
    public async Task<IActionResult> ExportReadings([FromQuery] int limit = 80640, [FromQuery] string? uid = null)
    {
        if (limit <= 0)
            return BadRequest(new { error = "limit must be greater than 0." });

        var readings = string.IsNullOrWhiteSpace(uid)
            ? await _sensorService.GetReadingsAsync(limit)
            : await _sensorService.GetReadingsAsync(limit, uid);

        var csv = _csvService.GenerateSensorReadingsCsv(readings);
        var fileName = $"sensor-readings-{DateTime.UtcNow:yyyyMMddHHmmss}.csv";

        return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", fileName);
    }

    [HttpGet("export/user")]
    public async Task<IActionResult> ExportUserReadings([FromQuery] string uid, [FromQuery] int limit = 80640)
    {
        if (limit <= 0)
            return BadRequest(new { error = "limit must be greater than 0." });

        if (string.IsNullOrWhiteSpace(uid))
            return BadRequest(new { error = "uid is required." });

        var readings = await _sensorService.GetReadingsAsync(limit, uid);
        var csv = _csvService.GenerateSensorReadingsCsv(readings);
        var fileName = $"sensor-readings-{uid}-{DateTime.UtcNow:yyyyMMddHHmmss}.csv";

        return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", fileName);
    }

    [HttpGet("export/sensor")]
    public async Task<IActionResult> ExportSensorReadings(
        [FromQuery] string uid,
        [FromQuery] string sensorId,
        [FromQuery] int limit = 80640)
    {
        if (limit <= 0)
            return BadRequest(new { error = "limit must be greater than 0." });

        if (string.IsNullOrWhiteSpace(uid) || string.IsNullOrWhiteSpace(sensorId))
            return BadRequest(new { error = "uid and sensorId are required." });

        var readings = await _sensorService.GetReadingsAsync(limit, uid, sensorId);
        if (readings is null)
            return NotFound(new { error = "Sensor was not found for this user." });

        var csv = _csvService.GenerateSensorReadingsCsv(readings);
        var fileName = $"sensor-readings-{sensorId}-{DateTime.UtcNow:yyyyMMddHHmmss}.csv";

        return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", fileName);
    }

    [HttpGet]
    [Route("daylyAverage")]
    public async Task<IActionResult> GetDaylyReadings([FromQuery] string? uid = null)
    {
        var readings = string.IsNullOrWhiteSpace(uid)
            ? await _sensorService.GetDaylyReadingsAsync()
            : await _sensorService.GetDaylyReadingsAsync(uid);

        var Average = AverageService.CalculateHourlyAverages(readings);
        return Ok(Average);
    }
    [HttpGet]
    [Route("weeklyAverage")]
    public async Task<IActionResult> GetWeeklyReadings([FromQuery] string? uid = null)
    {
        if (string.IsNullOrWhiteSpace(uid))
        {
            var averages = await _sensorService.GetWeeklyAveragesAsync();
            return Ok(averages);
        }

        var readings = await _sensorService.GetWeeklyReadingsAsync(uid);
        var average = AverageService.CalculateDailyAverages(readings);
        return Ok(average);
    }
    [HttpGet]
    [Route("monthlyAverage")]
    public async Task<IActionResult> GetMonthlyReadings([FromQuery] string? uid = null)
    {
        if (string.IsNullOrWhiteSpace(uid))
        {
            var averages = await _sensorService.GetMonthlyAveragesAsync();
            return Ok(averages);
        }

        var readings = await _sensorService.GetMonthlyReadingsAsync(uid);
        var average = AverageService.CalculateMonthlyAverages(readings);
        return Ok(average);
    }
}
