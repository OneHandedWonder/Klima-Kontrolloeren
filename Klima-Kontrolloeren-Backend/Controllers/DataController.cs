using Microsoft.AspNetCore.Mvc;
using KlimaKontrolloerenBackend.Models;
using KlimaKontrolloerenBackend.Services;

namespace KlimaKontrolloerenBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DataController : ControllerBase
{
    private readonly ISensorService _sensorService;
    public DataController(ISensorService sensorService)
    {
        _sensorService = sensorService;
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
        var readings = string.IsNullOrWhiteSpace(uid)
            ? await _sensorService.GetWeeklyReadingsAsync()
            : await _sensorService.GetWeeklyReadingsAsync(uid);

        var Average = AverageService.CalculateDailyAverages(readings);
        return Ok(Average);
    }
    [HttpGet]
    [Route("monthlyAverage")]
    public async Task<IActionResult> GetMonthlyReadings([FromQuery] string? uid = null)
    {
        var readings = string.IsNullOrWhiteSpace(uid)
            ? await _sensorService.GetMonthlyReadingsAsync()
            : await _sensorService.GetMonthlyReadingsAsync(uid);

        var Average = AverageService.CalculateWeeklyAverages(readings);
        return Ok(Average);
    }
}