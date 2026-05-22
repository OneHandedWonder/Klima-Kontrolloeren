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

    [Fact]
    public async Task GetReadingsAsync_WithUid_ReturnsFilteredList()
    {
        var expected = new List<SensorReading>
        {
            new() { Id = 1, SourceId = "uid-1", SensorId = "pi-sensor-01" }
        };

        _mockDb.Setup(x => x.GetReadingsAsync(25, "uid-1")).ReturnsAsync(expected);

        var result = await _sut.GetReadingsAsync(25, "uid-1");

        Assert.Same(expected, result);
    }

    [Theory]
    [InlineData(0, "uid-1")]
    [InlineData(10, "")]
    public async Task GetReadingsAsync_WithInvalidLimitOrUid_ThrowsArgumentException(int limit, string uid)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _sut.GetReadingsAsync(limit, uid));
    }

    [Fact]
    public async Task GetReadingsAsync_WithUidAndOwnedSensor_ReturnsSensorReadings()
    {
        var expected = new List<SensorReading>
        {
            new() { Id = 1, SensorId = "pi-sensor-01" }
        };

        _mockDb
            .Setup(x => x.GetKlimaDataUserAsync("uid-1"))
            .ReturnsAsync(new List<KlimaDataUser>
            {
                new() { Uid = "uid-1", Sensors = "[\"pi-sensor-01\",\"pi-sensor-02\"]", Enabled = true }
            });
        _mockDb.Setup(x => x.GetReadingsForSensorAsync(100, "pi-sensor-01")).ReturnsAsync(expected);

        var result = await _sut.GetReadingsAsync(100, "uid-1", "pi-sensor-01");

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task GetReadingsAsync_WithUidAndUnownedSensor_ReturnsNull()
    {
        _mockDb
            .Setup(x => x.GetKlimaDataUserAsync("uid-1"))
            .ReturnsAsync(new List<KlimaDataUser>
            {
                new() { Uid = "uid-1", Sensors = "[\"pi-sensor-01\"]", Enabled = true }
            });

        var result = await _sut.GetReadingsAsync(100, "uid-1", "other-sensor");

        Assert.Null(result);
        _mockDb.Verify(x => x.GetReadingsForSensorAsync(It.IsAny<int>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetDaylyReadingsAsync_ReturnsDatabaseResult()
    {
        var expected = new List<SensorReading> { new() { SensorId = "pi-sensor-01" } };
        _mockDb.Setup(x => x.GetDaylyReadingsAsync()).ReturnsAsync(expected);

        var result = await _sut.GetDaylyReadingsAsync();

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task GetDaylyReadingsAsync_WithUid_ReturnsDatabaseResult()
    {
        var expected = new List<SensorReading> { new() { SensorId = "pi-sensor-01" } };
        _mockDb.Setup(x => x.GetDaylyReadingsAsync("uid-1")).ReturnsAsync(expected);

        var result = await _sut.GetDaylyReadingsAsync("uid-1");

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task GetWeeklyReadingsAsync_ReturnsDatabaseResult()
    {
        var expected = new List<SensorReading> { new() { SensorId = "pi-sensor-01" } };
        _mockDb.Setup(x => x.GetWeeklyReadingsAsync()).ReturnsAsync(expected);

        var result = await _sut.GetWeeklyReadingsAsync();

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task GetWeeklyReadingsAsync_WithUid_ReturnsDatabaseResult()
    {
        var expected = new List<SensorReading> { new() { SensorId = "pi-sensor-01" } };
        _mockDb.Setup(x => x.GetWeeklyReadingsAsync("uid-1")).ReturnsAsync(expected);

        var result = await _sut.GetWeeklyReadingsAsync("uid-1");

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task GetWeeklyAveragesAsync_ReturnsDatabaseResult()
    {
        var expected = new List<AverageData> { new() { TimePeriod = DateTime.Today, AverageTemperature = 20 } };
        _mockDb.Setup(x => x.GetWeeklyAveragesAsync()).ReturnsAsync(expected);

        var result = await _sut.GetWeeklyAveragesAsync();

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task GetMonthlyAveragesAsync_ReturnsDatabaseResult()
    {
        var expected = new List<AverageData> { new() { TimePeriod = DateTime.Today, AverageTemperature = 20 } };
        _mockDb.Setup(x => x.GetMonthlyAveragesAsync()).ReturnsAsync(expected);

        var result = await _sut.GetMonthlyAveragesAsync();

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task GetMonthlyReadingsAsync_ReturnsDatabaseResult()
    {
        var expected = new List<SensorReading> { new() { SensorId = "pi-sensor-01" } };
        _mockDb.Setup(x => x.GetMonthlyReadingsAsync()).ReturnsAsync(expected);

        var result = await _sut.GetMonthlyReadingsAsync();

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task GetMonthlyReadingsAsync_WithUid_ReturnsDatabaseResult()
    {
        var expected = new List<SensorReading> { new() { SensorId = "pi-sensor-01" } };
        _mockDb.Setup(x => x.GetMonthlyReadingsAsync("uid-1")).ReturnsAsync(expected);

        var result = await _sut.GetMonthlyReadingsAsync("uid-1");

        Assert.Same(expected, result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task TimeRangeMethods_WithInvalidUid_ThrowArgumentException(string uid)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _sut.GetDaylyReadingsAsync(uid));
        await Assert.ThrowsAsync<ArgumentException>(() => _sut.GetWeeklyReadingsAsync(uid));
        await Assert.ThrowsAsync<ArgumentException>(() => _sut.GetMonthlyReadingsAsync(uid));
    }

    [Fact]
    public async Task GetUserSensorsAsync_ReturnsDatabaseResult()
    {
        var expected = new List<KlimaDataUser> { new() { Uid = "uid-1", Enabled = true } };
        _mockDb.Setup(x => x.GetKlimaDataUserAsync("uid-1")).ReturnsAsync(expected);

        var result = await _sut.GetUserSensorsAsync("uid-1");

        Assert.Same(expected, result);
    }
}
