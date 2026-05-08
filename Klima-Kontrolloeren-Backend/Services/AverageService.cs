using KlimaKontrolloerenBackend.Data;
using KlimaKontrolloerenBackend.Models;
using System.Globalization;

namespace KlimaKontrolloerenBackend.Services;

public class AverageService
{
    public static List<AverageData> CalculateWeeklyAverages(List<SensorReading> readings)
    {
        // Build last 30 week periods (start date for each week, Monday as start)
        var now = DateTime.Now.Date;
        int totalWeeks = 30;
        // find start of current week (Monday)
        int diff = ((int)now.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        var currentWeekStart = now.AddDays(-diff);
        var weekStarts = Enumerable.Range(0, totalWeeks)
            .Select(i => currentWeekStart.AddDays(-7 * (totalWeeks - 1 - i)))
            .ToList();

        var byWeek = readings.GroupBy(r =>
        {
            var d = r.RecordedAt.Date;
            int dDiff = ((int)d.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
            return d.AddDays(-dDiff);
        }).ToDictionary(g => g.Key, g => g.ToList());

        var result = weekStarts.Select((ws, index) =>
        {
            byWeek.TryGetValue(ws, out var list);
            if (list != null && list.Count > 0)
            {
                return new AverageData
                {
                    TimePeriod = ws,
                    ReadingCount = index + 1,
                    AverageTemperature = list.Average(r => r.Temperature),
                    AverageHumidity = list.Average(r => r.Humidity),
                    AverageCO2PPM = list.Average(r => r.CO2PPM)
                };
            }

            return new AverageData
            {
                TimePeriod = ws,
                ReadingCount = index + 1,
                AverageTemperature = null,
                AverageHumidity = null,
                AverageCO2PPM = null
            };
        }).ToList();

        return result;
    }

    public static List<AverageData> CalculateDailyAverages(List<SensorReading> readings)
    {
        int totalDays = 7;
        var today = DateTime.Today;
        var dayStarts = Enumerable.Range(0, totalDays)
            .Select(i => today.AddDays(-(totalDays - 1 - i)))
            .ToList();

        var byDay = readings.GroupBy(r => r.RecordedAt.Date).ToDictionary(g => g.Key, g => g.ToList());

        var result = dayStarts.Select((ds, index) =>
        {
            byDay.TryGetValue(ds, out var list);
            if (list != null && list.Count > 0)
            {
                return new AverageData
                {
                    TimePeriod = ds,
                    ReadingCount = index + 1,
                    AverageTemperature = list.Average(r => r.Temperature),
                    AverageHumidity = list.Average(r => r.Humidity),
                    AverageCO2PPM = list.Average(r => r.CO2PPM)
                };
            }

            return new AverageData
            {
                TimePeriod = ds,
                ReadingCount = index + 1,
                AverageTemperature = null,
                AverageHumidity = null,
                AverageCO2PPM = null
            };
        }).ToList();

        return result;
    }

    public static List<AverageData> CalculateHourlyAverages(List<SensorReading> readings)
    {
        int totalHours = 24;
        var now = DateTime.Now;
        var currentHourStart = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0);
        var hourStarts = Enumerable.Range(0, totalHours)
            .Select(i => currentHourStart.AddHours(-(totalHours - 1 - i)))
            .ToList();

        var byHour = readings.GroupBy(r => new DateTime(r.RecordedAt.Year, r.RecordedAt.Month, r.RecordedAt.Day, r.RecordedAt.Hour, 0, 0))
            .ToDictionary(g => g.Key, g => g.ToList());

        var result = hourStarts.Select((hs, index) =>
        {
            byHour.TryGetValue(hs, out var list);
            if (list != null && list.Count > 0)
            {
                return new AverageData
                {
                    TimePeriod = hs,
                    ReadingCount = index + 1,
                    AverageTemperature = list.Average(r => r.Temperature),
                    AverageHumidity = list.Average(r => r.Humidity),
                    AverageCO2PPM = list.Average(r => r.CO2PPM)
                };
            }

            return new AverageData
            {
                TimePeriod = hs,
                ReadingCount = index + 1,
                AverageTemperature = null,
                AverageHumidity = null,
                AverageCO2PPM = null
            };
        }).ToList();

        return result;
    }
}