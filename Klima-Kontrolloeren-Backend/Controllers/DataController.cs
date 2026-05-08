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
    public async Task<IActionResult> GetReadings([FromQuery] int limit = 80640)
    {
        if (limit <= 0)
            return BadRequest(new { error = "limit must be greater than 0." });

        var readings = await _sensorService.GetReadingsAsync(limit);
        return Ok(readings);
    }
    [HttpGet]
    [Route("daylyAverage")]
    public async Task<IActionResult> GetDaylyReadings()
    {
        var readings = await _sensorService.GetDaylyReadingsAsync();
        var Average = AverageService.CalculateHourlyAverages(readings);
        return Ok(Average);
    }
    [HttpGet]
    [Route("weeklyAverage")]
    public async Task<IActionResult> GetWeeklyReadings()
    {
        var readings = await _sensorService.GetWeeklyReadingsAsync();
        var Average = AverageService.CalculateDailyAverages(readings);
        return Ok(Average);
    }
    [HttpGet]
    [Route("monthlyAverage")]
    public async Task<IActionResult> GetMonthlyReadings()
    {
        var readings = await _sensorService.GetMonthlyReadingsAsync();
        var Average = AverageService.CalculateWeeklyAverages(readings);
        return Ok(Average);
    }
}