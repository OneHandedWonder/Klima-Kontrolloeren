using KlimaKontrolloerenBackend.Controllers;
using KlimaKontrolloerenBackend.Models;
using KlimaKontrolloerenBackend.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace KlimaKontrolloerenBackend.Tests.Controllers;

public class DataControllerTests
{
    private readonly Mock<ISensorService> _sensorService = new();
    private readonly Mock<ICsvService> _csvService = new();
    private readonly DataController _sut;

    public DataControllerTests()
    {
        _sut = new DataController(_sensorService.Object, _csvService.Object);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public async Task GetReadings_InvalidLimit_Returns400(int limit)
    {
        var result = await _sut.GetReadings(limit);

        Assert.IsType<BadRequestObjectResult>(result);
        _sensorService.Verify(x => x.GetReadingsAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetReadings_WithoutUid_ReturnsReadingsFromService()
    {
        var readings = new List<SensorReading>
        {
            new() { Id = 1, SensorId = "pi-sensor-01", Temperature = 22.5, Humidity = 58, CO2PPM = 420 }
        };

        _sensorService.Setup(x => x.GetReadingsAsync(100)).ReturnsAsync(readings);

        var result = await _sut.GetReadings(100);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(readings, ok.Value);
    }

    [Fact]
    public async Task GetReadings_WithUid_UsesUserFilteredServiceCall()
    {
        var readings = new List<SensorReading>
        {
            new() { Id = 1, SensorId = "pi-sensor-01", Temperature = 21, Humidity = 60, CO2PPM = 410 }
        };

        _sensorService.Setup(x => x.GetReadingsAsync(50, "uid-1")).ReturnsAsync(readings);

        var result = await _sut.GetReadings(50, "uid-1");

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(readings, ok.Value);
        _sensorService.Verify(x => x.GetReadingsAsync(50, "uid-1"), Times.Once);
    }

    [Fact]
    public async Task ExportReadings_ReturnsCsvFile()
    {
        var readings = new List<SensorReading>
        {
            new() { Id = 1, SensorId = "pi-sensor-01", Temperature = 21, Humidity = 60, CO2PPM = 410 }
        };

        _sensorService.Setup(x => x.GetReadingsAsync(100)).ReturnsAsync(readings);
        _csvService.Setup(x => x.GenerateSensorReadingsCsv(readings)).Returns("Id,SensorId\n1,pi-sensor-01\n");

        var result = await _sut.ExportReadings(100);

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("text/csv", file.ContentType);
        Assert.EndsWith(".csv", file.FileDownloadName);
        Assert.Equal("Id,SensorId\n1,pi-sensor-01\n", System.Text.Encoding.UTF8.GetString(file.FileContents));
    }

    [Fact]
    public async Task ExportUserReadings_WithUid_ReturnsCsvForUserOwnedData()
    {
        var readings = new List<SensorReading>
        {
            new() { Id = 1, SensorId = "pi-sensor-01" }
        };

        _sensorService.Setup(x => x.GetReadingsAsync(100, "uid-1")).ReturnsAsync(readings);
        _csvService.Setup(x => x.GenerateSensorReadingsCsv(readings)).Returns("csv");

        var result = await _sut.ExportUserReadings("uid-1", 100);

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("text/csv", file.ContentType);
        Assert.Contains("uid-1", file.FileDownloadName);
        _sensorService.Verify(x => x.GetReadingsAsync(100, "uid-1"), Times.Once);
    }

    [Fact]
    public async Task ExportSensorReadings_WhenUserOwnsSensor_ReturnsCsvFile()
    {
        var readings = new List<SensorReading>
        {
            new() { Id = 1, SensorId = "pi-sensor-01" }
        };

        _sensorService.Setup(x => x.GetReadingsAsync(100, "uid-1", "pi-sensor-01")).ReturnsAsync(readings);
        _csvService.Setup(x => x.GenerateSensorReadingsCsv(readings)).Returns("csv");

        var result = await _sut.ExportSensorReadings("uid-1", "pi-sensor-01", 100);

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("text/csv", file.ContentType);
        Assert.Contains("pi-sensor-01", file.FileDownloadName);
    }

    [Fact]
    public async Task ExportSensorReadings_WhenUserDoesNotOwnSensor_Returns404()
    {
        _sensorService
            .Setup(x => x.GetReadingsAsync(100, "uid-1", "other-sensor"))
            .ReturnsAsync((List<SensorReading>?)null);

        var result = await _sut.ExportSensorReadings("uid-1", "other-sensor", 100);

        Assert.IsType<NotFoundObjectResult>(result);
        _csvService.Verify(x => x.GenerateSensorReadingsCsv(It.IsAny<IEnumerable<SensorReading>>()), Times.Never);
    }

    [Fact]
    public async Task GetDaylyReadings_Returns24HourlyAverageBuckets()
    {
        var now = DateTime.Now;
        var hour = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0);
        _sensorService.Setup(x => x.GetDaylyReadingsAsync()).ReturnsAsync(new List<SensorReading>
        {
            new() { RecordedAt = hour, Temperature = 20, Humidity = 40, CO2PPM = 400 },
            new() { RecordedAt = hour.AddMinutes(10), Temperature = 22, Humidity = 50, CO2PPM = 420 }
        });

        var result = await _sut.GetDaylyReadings();

        var ok = Assert.IsType<OkObjectResult>(result);
        var averages = Assert.IsType<List<AverageData>>(ok.Value);
        Assert.Equal(24, averages.Count);
        var currentHour = averages.Single(x => x.TimePeriod == hour);
        Assert.Equal(21, currentHour.AverageTemperature);
        Assert.Equal(45, currentHour.AverageHumidity);
        Assert.Equal(410, currentHour.AverageCO2PPM);
    }

    [Fact]
    public async Task GetDaylyReadings_WithUid_UsesUserFilteredServiceCall()
    {
        _sensorService.Setup(x => x.GetDaylyReadingsAsync("uid-1")).ReturnsAsync(new List<SensorReading>());

        var result = await _sut.GetDaylyReadings("uid-1");

        var ok = Assert.IsType<OkObjectResult>(result);
        var averages = Assert.IsType<List<AverageData>>(ok.Value);
        Assert.Equal(24, averages.Count);
        _sensorService.Verify(x => x.GetDaylyReadingsAsync("uid-1"), Times.Once);
    }

    [Fact]
    public async Task GetWeeklyReadings_ReturnsSevenDailyAverageBuckets()
    {
        var expected = new List<AverageData>
        {
            new() { TimePeriod = DateTime.Today, AverageTemperature = 20, AverageHumidity = 45, AverageCO2PPM = 420 }
        };

        _sensorService.Setup(x => x.GetWeeklyAveragesAsync()).ReturnsAsync(expected);

        var result = await _sut.GetWeeklyReadings();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(expected, ok.Value);
        _sensorService.Verify(x => x.GetWeeklyAveragesAsync(), Times.Once);
        _sensorService.Verify(x => x.GetWeeklyReadingsAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetWeeklyReadings_WithUid_UsesUserFilteredServiceCall()
    {
        _sensorService.Setup(x => x.GetWeeklyReadingsAsync("uid-1")).ReturnsAsync(new List<SensorReading>());

        var result = await _sut.GetWeeklyReadings("uid-1");

        var ok = Assert.IsType<OkObjectResult>(result);
        var averages = Assert.IsType<List<AverageData>>(ok.Value);
        Assert.Equal(7, averages.Count);
        _sensorService.Verify(x => x.GetWeeklyReadingsAsync("uid-1"), Times.Once);
    }

    [Fact]
    public async Task GetMonthlyReadings_ReturnsThirtyDailyAverageBuckets()
    {
        var expected = new List<AverageData>
        {
            new() { TimePeriod = DateTime.Today, AverageTemperature = 22, AverageHumidity = 50, AverageCO2PPM = 440 }
        };

        _sensorService.Setup(x => x.GetMonthlyAveragesAsync()).ReturnsAsync(expected);

        var result = await _sut.GetMonthlyReadings();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(expected, ok.Value);
        _sensorService.Verify(x => x.GetMonthlyAveragesAsync(), Times.Once);
        _sensorService.Verify(x => x.GetMonthlyReadingsAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetMonthlyReadings_WithUid_UsesUserFilteredServiceCall()
    {
        _sensorService.Setup(x => x.GetMonthlyReadingsAsync("uid-1")).ReturnsAsync(new List<SensorReading>());

        var result = await _sut.GetMonthlyReadings("uid-1");

        var ok = Assert.IsType<OkObjectResult>(result);
        var averages = Assert.IsType<List<AverageData>>(ok.Value);
        Assert.Equal(30, averages.Count);
        _sensorService.Verify(x => x.GetMonthlyReadingsAsync("uid-1"), Times.Once);
    }
}
