using Moq;
using KlimaKontrolloerenBackend.Data;
using KlimaKontrolloerenBackend.Models;
using KlimaKontrolloerenBackend.Services;
using Xunit;

namespace KlimaKontrolloerenBackend.Tests.Services;

public class SensorServiceTests
{
    private readonly Mock<IDbConnectionFactory> _mockDb;
    private readonly SensorService _sut;

    public SensorServiceTests()
    {
        _mockDb = new Mock<IDbConnectionFactory>();
        _sut = new SensorService(_mockDb.Object);  // SensorService only takes IDbConnectionFactory
    }

    [Fact]
    public async Task SaveReadingAsync_ValidInput_CallsDbOnce()
    {
        // Arrange
        var dto = new SensorReadingDto
        {
            SensorId = "pi-sensor-01",
            Temperature = 22.5,
            Humidity = 60.0,
            CO2PPM = 415.0
        };

        _mockDb
            .Setup(x => x.SaveReadingAsync(dto))
            .Returns(Task.CompletedTask);

        // Act
        await _sut.SaveReadingAsync(dto);

        // Assert
        _mockDb.Verify(x => x.SaveReadingAsync(dto), Times.Once);
    }

    [Fact]
    public async Task GetReadingsAsync_ValidLimit_ReturnsList()
    {
        // Arrange
        var expected = new List<SensorReading>
        {
            new() { Id = 1, SourceId = "device-123", SensorId = "pi-sensor-01",
                    Temperature = 22.5, Humidity = 60.0, CO2PPM = 415.0 },
            new() { Id = 2, SourceId = "device-123", SensorId = "pi-sensor-01",
                    Temperature = 23.1, Humidity = 58.5, CO2PPM = 420.0 }
        };

        _mockDb
            .Setup(x => x.GetReadingsAsync(100))
            .ReturnsAsync(expected);

        // Act
        var result = await _sut.GetReadingsAsync(100);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.All(result, r => Assert.Equal("device-123", r.SourceId));
    }

    [Fact]
    public async Task GetReadingsAsync_NoReadings_ReturnsEmptyList()
    {
        // Arrange
        _mockDb
            .Setup(x => x.GetReadingsAsync(100))
            .ReturnsAsync(new List<SensorReading>());

        // Act
        var result = await _sut.GetReadingsAsync(100);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task GetReadingsAsync_InvalidLimit_ThrowsArgumentException(int invalidLimit)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.GetReadingsAsync(invalidLimit));
    }
}