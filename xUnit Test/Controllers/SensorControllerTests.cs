using KlimaKontrolloerenBackend.Controllers;
using KlimaKontrolloerenBackend.Models;
using KlimaKontrolloerenBackend.Services;
using TestProject.Helpers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace KlimaKontrolloerenBackend.Tests.Controllers;

public class SensorControllerTests
{
    private readonly Mock<ISensorService> _mockSensorService;
    private readonly SensorController _sut;

    public SensorControllerTests()
    {
        _mockSensorService = new Mock<ISensorService>();
        _sut = new SensorController(_mockSensorService.Object);
    }

    // -------------------------
    // POST /api/sensor
    // -------------------------

    [Fact]
    public async Task PostReading_ValidTokenAndDto_Returns201()
    {
        // Arrange
        var dto = new SensorReadingDto
        {
            SensorId = "pi-sensor-01",
            Temperature = 22.5,
            Humidity = 60.0,
            CO2PPM = 415.0
        };

        _mockSensorService
            .Setup(x => x.SaveReadingAsync(dto))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.PostReading(dto);

        // Assert
        var created = Assert.IsType<CreatedResult>(result);
        Assert.Equal(201, created.StatusCode);
    }

    [Fact]
    public async Task PostReading_ValidDto_Returns201()
    {
        // Arrange
        var result = await _sut.PostReading(new SensorReadingDto { SensorId = "pi-sensor-01" });

        // Assert
        Assert.IsType<CreatedResult>(result);
    }

    [Fact]
    public async Task PostReading_MissingSensorId_Returns400()
    {
        // Arrange
        // Act
        var result = await _sut.PostReading(new SensorReadingDto { SensorId = "" });

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    // -------------------------
    // GET /api/sensor
    // -------------------------

    [Fact]
    public async Task GetReadings_ValidToken_Returns200WithData()
    {
        // Arrange
        var readings = new List<SensorReading>
        {
            new() { Id = 1, SourceId = "device-123", SensorId = "pi-sensor-01",
                    Temperature = 22.5, Humidity = 60.0, CO2PPM = 415.0 }
        };

        _mockSensorService
            .Setup(x => x.GetReadingsAsync(100))
            .ReturnsAsync(readings);

        // Act
        var result = await _sut.GetReadings(100);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        var data = Assert.IsType<List<SensorReading>>(ok.Value);
        Assert.Single(data);
    }

    [Fact]
    public async Task GetReadings_Returns200WithData()
    {
        // Arrange
        var result = await _sut.GetReadings();

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetReadings_NoReadingsExist_Returns200WithEmptyList()
    {
        // Arrange
        _mockSensorService
            .Setup(x => x.GetReadingsAsync(100))
            .ReturnsAsync(new List<SensorReading>());

        // Act
        var result = await _sut.GetReadings();

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        var data = Assert.IsType<List<SensorReading>>(ok.Value);
        Assert.Empty(data);
    }
}