using KlimaKontrolloerenBackend.Controllers;
using KlimaKontrolloerenBackend.Models;
using KlimaKontrolloerenBackend.Services;
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
    public async Task GetUserSensors_MissingUid_Returns400()
    {
        // Act
        var result = await _sut.GetUserSensors("");

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
        _mockSensorService.Verify(x => x.GetUserSensorsAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetUserSensors_NoSensorsFound_Returns404()
    {
        // Arrange
        _mockSensorService
            .Setup(x => x.GetUserSensorsAsync("uid-1"))
            .ReturnsAsync(new List<KlimaDataUser>());

        // Act
        var result = await _sut.GetUserSensors("uid-1");

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Theory]
    [InlineData("[\"pi-sensor-01\",\"pi-sensor-02\"]")]
    [InlineData("\"pi-sensor-01\", \"pi-sensor-02\"")]
    [InlineData("\"pi-sensor-01\"")]
    public async Task GetUserSensors_WithStoredSensors_ReturnsParsedSensors(string storedSensors)
    {
        // Arrange
        _mockSensorService
            .Setup(x => x.GetUserSensorsAsync("uid-1"))
            .ReturnsAsync(new List<KlimaDataUser>
            {
                new() { Uid = "uid-1", Sensors = storedSensors, Enabled = true }
            });

        // Act
        var result = await _sut.GetUserSensors("uid-1");

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        var item = Assert.Single(Assert.IsAssignableFrom<IEnumerable<object>>(ok.Value));
        var sensors = Assert.IsAssignableFrom<string[]>(
            item.GetType().GetProperty("sensors")!.GetValue(item));

        Assert.Contains("pi-sensor-01", sensors);
    }

    [Fact]
    public async Task GetUserSensors_WithBlankStoredSensors_ReturnsEmptySensorsArray()
    {
        // Arrange
        _mockSensorService
            .Setup(x => x.GetUserSensorsAsync("uid-1"))
            .ReturnsAsync(new List<KlimaDataUser>
            {
                new() { Uid = "uid-1", Sensors = "", Enabled = true }
            });

        // Act
        var result = await _sut.GetUserSensors("uid-1");

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        var item = Assert.Single(Assert.IsAssignableFrom<IEnumerable<object>>(ok.Value));
        var sensors = Assert.IsAssignableFrom<string[]>(
            item.GetType().GetProperty("sensors")!.GetValue(item));

        Assert.Empty(sensors);
    }
}
