using KlimaKontrolloerenBackend.Models;
using KlimaKontrolloerenBackend.Services;
using Xunit;

namespace KlimaKontrolloerenBackend.Tests.Services;

public class CsvServiceTests
{
    private readonly CsvService _sut = new();

    [Fact]
    public void GenerateSensorReadingsCsv_ReturnsHeaderAndRows()
    {
        var readings = new List<SensorReading>
        {
            new()
            {
                Id = 1,
                SourceId = "uid-1",
                SensorId = "sensor-1",
                Temperature = 22.5,
                Humidity = 55.2,
                CO2PPM = 420,
                RecordedAt = new DateTime(2026, 5, 22, 10, 30, 0, DateTimeKind.Utc)
            }
        };

        var csv = _sut.GenerateSensorReadingsCsv(readings);

        Assert.Contains("Id,SourceId,SensorId,Temperature,Humidity,CO2PPM,RecordedAt", csv);
        Assert.Contains("1,uid-1,sensor-1,22.5,55.2,420,2026-05-22T10:30:00.0000000Z", csv);
    }

    [Fact]
    public void GenerateSensorReadingsCsv_EscapesCsvValues()
    {
        var readings = new List<SensorReading>
        {
            new() { SourceId = "uid,1", SensorId = "sensor \"one\"" }
        };

        var csv = _sut.GenerateSensorReadingsCsv(readings);

        Assert.Contains("\"uid,1\",\"sensor \"\"one\"\"\"", csv);
    }
}
