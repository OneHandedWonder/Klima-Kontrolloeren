using KlimaKontrolloerenBackend.Models;
using KlimaKontrolloerenBackend.Services;
using Xunit;

namespace KlimaKontrolloerenBackend.Tests.Services;

public class AverageServiceTests
{
    [Fact]
    public void CalculateDailyAverages_ReturnsSevenDaysWithExpectedAverage()
    {
        var today = DateTime.Today;
        var readings = new List<SensorReading>
        {
            new() { RecordedAt = today.AddHours(8), Temperature = 20, Humidity = 40, CO2PPM = 400 },
            new() { RecordedAt = today.AddHours(9), Temperature = 24, Humidity = 60, CO2PPM = 440 }
        };

        var result = AverageService.CalculateDailyAverages(readings);

        Assert.Equal(7, result.Count);
        var todayAverage = result.Single(x => x.TimePeriod == today);
        Assert.Equal(22, todayAverage.AverageTemperature);
        Assert.Equal(50, todayAverage.AverageHumidity);
        Assert.Equal(420, todayAverage.AverageCO2PPM);
    }

    [Fact]
    public void CalculateWeeklyAverages_ReturnsThirtyDaysAndNullsForMissingDays()
    {
        var result = AverageService.CalculateWeeklyAverages(new List<SensorReading>());

        Assert.Equal(30, result.Count);
        Assert.All(result, average =>
        {
            Assert.Null(average.AverageTemperature);
            Assert.Null(average.AverageHumidity);
            Assert.Null(average.AverageCO2PPM);
        });
    }

    [Fact]
    public void CalculateHourlyAverages_ReturnsTwentyFourHoursWithExpectedAverage()
    {
        var now = DateTime.Now;
        var currentHour = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0);
        var readings = new List<SensorReading>
        {
            new() { RecordedAt = currentHour, Temperature = 18, Humidity = 40, CO2PPM = 400 },
            new() { RecordedAt = currentHour.AddMinutes(30), Temperature = 22, Humidity = 60, CO2PPM = 440 }
        };

        var result = AverageService.CalculateHourlyAverages(readings);

        Assert.Equal(24, result.Count);
        var currentHourAverage = result.Single(x => x.TimePeriod == currentHour);
        Assert.Equal(20, currentHourAverage.AverageTemperature);
        Assert.Equal(50, currentHourAverage.AverageHumidity);
        Assert.Equal(420, currentHourAverage.AverageCO2PPM);
    }
}
