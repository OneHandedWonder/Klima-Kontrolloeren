using KlimaKontrolloerenBackend.Models;
using Xunit;

namespace KlimaKontrolloerenBackend.Tests.Models;

public class SensorModelsTests
{
    [Fact]
    public void SensorReading_DefaultValues_AreExpected()
    {
        var model = new SensorReading();

        Assert.Equal(0, model.Id);
        Assert.Equal(string.Empty, model.SourceId);
        Assert.Equal(string.Empty, model.SensorId);
        Assert.Equal(0, model.Temperature);
        Assert.Equal(0, model.Humidity);
        Assert.Equal(0, model.CO2PPM);
        Assert.Equal(default, model.RecordedAt);
    }

    [Fact]
    public void SensorReading_CanStoreAssignedValues()
    {
        var recordedAt = new DateTime(2026, 5, 18, 10, 30, 0);
        var model = new SensorReading
        {
            Id = 1,
            SourceId = "uid-1",
            SensorId = "pi-sensor-01",
            Temperature = 22.5,
            Humidity = 61.2,
            CO2PPM = 415.8,
            RecordedAt = recordedAt
        };

        Assert.Equal(1, model.Id);
        Assert.Equal("uid-1", model.SourceId);
        Assert.Equal("pi-sensor-01", model.SensorId);
        Assert.Equal(22.5, model.Temperature);
        Assert.Equal(61.2, model.Humidity);
        Assert.Equal(415.8, model.CO2PPM);
        Assert.Equal(recordedAt, model.RecordedAt);
    }

    [Fact]
    public void SensorReadingDto_CanStoreAssignedValues()
    {
        var model = new SensorReadingDto
        {
            SensorId = "pi-sensor-01",
            Temperature = 21.4,
            Humidity = 55.5,
            CO2PPM = 430.1
        };

        Assert.Equal("pi-sensor-01", model.SensorId);
        Assert.Equal(21.4, model.Temperature);
        Assert.Equal(55.5, model.Humidity);
        Assert.Equal(430.1, model.CO2PPM);
    }

    [Fact]
    public void AverageData_CanStoreNullableAverageValues()
    {
        var timePeriod = new DateTime(2026, 5, 18);
        var model = new AverageData
        {
            TimePeriod = timePeriod,
            ReadingCount = 5,
            AverageTemperature = 22.2,
            AverageHumidity = null,
            AverageCO2PPM = 410.7
        };

        Assert.Equal(timePeriod, model.TimePeriod);
        Assert.Equal(5, model.ReadingCount);
        Assert.Equal(22.2, model.AverageTemperature);
        Assert.Null(model.AverageHumidity);
        Assert.Equal(410.7, model.AverageCO2PPM);
    }
}
