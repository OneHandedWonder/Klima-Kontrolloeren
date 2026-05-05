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
    private readonly Mock<IFirebaseService> _mockFirebaseService;
    private readonly SensorController _sut;

    public SensorControllerTests()
    {
        _mockSensorService = new Mock<ISensorService>();
        _mockFirebaseService = new Mock<IFirebaseService>();
        _sut = new SensorController(_mockSensorService.Object, _mockFirebaseService.Object);
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

        MockHelpers.SetAuthHeader(_sut, "valid-token");

        _mockFirebaseService
            .Setup(x => x.VerifyTokenAsync("valid-token"))
            .ReturnsAsync("uid-123");

        _mockSensorService
            .Setup(x => x.SaveReadingAsync("uid-123", dto))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.PostReading(dto);

        // Assert
        var created = Assert.IsType<CreatedResult>(result);
        Assert.Equal(201, created.StatusCode);
    }

    [Fact]
    public async Task PostReading_InvalidToken_Returns401()
    {
        // Arrange
        MockHelpers.SetAuthHeader(_sut, "bad-token");

        _mockFirebaseService
            .Setup(x => x.VerifyTokenAsync("bad-token"))
            .ReturnsAsync((string?)null);

        // Act
        var result = await _sut.PostReading(new SensorReadingDto { SensorId = "pi-sensor-01" });

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task PostReading_MissingAuthHeader_Returns401()
    {
        // Arrange
        MockHelpers.SetNoAuthHeader(_sut);

        // Act
        var result = await _sut.PostReading(new SensorReadingDto { SensorId = "pi-sensor-01" });

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task PostReading_MissingSensorId_Returns400()
    {
        // Arrange
        MockHelpers.SetAuthHeader(_sut, "valid-token");

        _mockFirebaseService
            .Setup(x => x.VerifyTokenAsync("valid-token"))
            .ReturnsAsync("uid-123");

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
        MockHelpers.SetAuthHeader(_sut, "valid-token");

        _mockFirebaseService
            .Setup(x => x.VerifyTokenAsync("valid-token"))
            .ReturnsAsync("uid-123");

        var readings = new List<SensorReading>
        {
            new() { Id = 1, FirebaseUID = "uid-123", SensorId = "pi-sensor-01",
                    Temperature = 22.5, Humidity = 60.0, CO2PPM = 415.0 }
        };

        _mockSensorService
            .Setup(x => x.GetReadingsAsync("uid-123", 100))
            .ReturnsAsync(readings);

        // Act
        var result = await _sut.GetReadings(100);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        var data = Assert.IsType<List<SensorReading>>(ok.Value);
        Assert.Single(data);
    }

    [Fact]
    public async Task GetReadings_InvalidToken_Returns401()
    {
        // Arrange
        MockHelpers.SetAuthHeader(_sut, "bad-token");

        _mockFirebaseService
            .Setup(x => x.VerifyTokenAsync("bad-token"))
            .ReturnsAsync((string?)null);

        // Act
        var result = await _sut.GetReadings();

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task GetReadings_NoReadingsExist_Returns200WithEmptyList()
    {
        // Arrange
        MockHelpers.SetAuthHeader(_sut, "valid-token");

        _mockFirebaseService
            .Setup(x => x.VerifyTokenAsync("valid-token"))
            .ReturnsAsync("uid-123");

        _mockSensorService
            .Setup(x => x.GetReadingsAsync("uid-123", 100))
            .ReturnsAsync(new List<SensorReading>());

        // Act
        var result = await _sut.GetReadings();

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        var data = Assert.IsType<List<SensorReading>>(ok.Value);
        Assert.Empty(data);
    }
}