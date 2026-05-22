using KlimaKontrolloerenBackend.Data;
using KlimaKontrolloerenBackend.Models;

namespace KlimaKontrolloerenBackend.Services;

public class SensorService : ISensorService
{
    private readonly IDbConnectionFactory _db;

    public SensorService(IDbConnectionFactory db)
    {
        _db = db;
    }

    public async Task SaveReadingAsync(SensorReadingDto dto)
    {
        await _db.SaveReadingAsync(dto);
    }

    public async Task<List<SensorReading>> GetReadingsAsync(int limit = 100)
    {
        if (limit <= 0)
            throw new ArgumentException("Limit must be greater than 0.", nameof(limit));

        return await _db.GetReadingsAsync(limit);
    }

    public async Task<List<SensorReading>> GetReadingsAsync(int limit, string uid)
    {
        if (limit <= 0)
            throw new ArgumentException("Limit must be greater than 0.", nameof(limit));
        
        if (string.IsNullOrWhiteSpace(uid))
            throw new ArgumentException("UID cannot be empty.", nameof(uid));

        return await _db.GetReadingsAsync(limit, uid);
    }

    public async Task<List<SensorReading>?> GetReadingsAsync(int limit, string uid, string sensorId)
    {
        if (limit <= 0)
            throw new ArgumentException("Limit must be greater than 0.", nameof(limit));

        if (string.IsNullOrWhiteSpace(uid))
            throw new ArgumentException("UID cannot be empty.", nameof(uid));

        if (string.IsNullOrWhiteSpace(sensorId))
            throw new ArgumentException("Sensor ID cannot be empty.", nameof(sensorId));

        var users = await _db.GetKlimaDataUserAsync(uid);
        var userOwnsSensor = users
            .SelectMany(user => ParseSensors(user.Sensors))
            .Any(sensor => string.Equals(sensor, sensorId, StringComparison.OrdinalIgnoreCase));

        if (!userOwnsSensor)
            return null;

        return await _db.GetReadingsForSensorAsync(limit, sensorId);
    }

    public async Task<List<SensorReading>> GetDaylyReadingsAsync()
    {
        return await _db.GetDaylyReadingsAsync();
    }

    public async Task<List<SensorReading>> GetDaylyReadingsAsync(string uid)
    {
        if (string.IsNullOrWhiteSpace(uid))
            throw new ArgumentException("UID cannot be empty.", nameof(uid));

        return await _db.GetDaylyReadingsAsync(uid);
    }

    public async Task<List<SensorReading>> GetWeeklyReadingsAsync()
    {
        return await _db.GetWeeklyReadingsAsync();
    }

    public async Task<List<SensorReading>> GetWeeklyReadingsAsync(string uid)
    {
        if (string.IsNullOrWhiteSpace(uid))
            throw new ArgumentException("UID cannot be empty.", nameof(uid));

        return await _db.GetWeeklyReadingsAsync(uid);
    }

    public async Task<List<AverageData>> GetWeeklyAveragesAsync()
    {
        return await _db.GetWeeklyAveragesAsync();
    }

    public async Task<List<AverageData>> GetMonthlyAveragesAsync()
    {
        return await _db.GetMonthlyAveragesAsync();
    }

    public async Task<List<SensorReading>> GetMonthlyReadingsAsync()
    {
        return await _db.GetMonthlyReadingsAsync();
    }

    public async Task<List<SensorReading>> GetMonthlyReadingsAsync(string uid)
    {
        if (string.IsNullOrWhiteSpace(uid))
            throw new ArgumentException("UID cannot be empty.", nameof(uid));

        return await _db.GetMonthlyReadingsAsync(uid);
    }

    public async Task<List<KlimaDataUser>> GetUserSensorsAsync(string uid)
    {
        return await _db.GetKlimaDataUserAsync(uid);
    }

    public async Task<SensorInfoResult?> GetSensorInfoAsync(string uid, string sensorId)
        => await _db.GetSensorInfoAsync(uid, sensorId);

    public async Task UpdateSensorInfoAsync(string uid, string sensorId, string name, string type, string location)
        => await _db.UpdateSensorInfoAsync(uid, sensorId, name, type, location);

    public async Task AddSensorToUserAsync(string uid, string sensorId)
        => await _db.AddSensorToUserAsync(uid, sensorId);

    public async Task RemoveSensorFromUserAsync(string uid, string sensorId)
        => await _db.RemoveSensorFromUserAsync(uid, sensorId);

    private static List<string> ParseSensors(string sensorsString)
    {
        if (string.IsNullOrWhiteSpace(sensorsString))
            return new List<string>();

        try
        {
            var parsed = System.Text.Json.JsonSerializer.Deserialize<string[]>(sensorsString);
            return parsed?.ToList() ?? new List<string>();
        }
        catch
        {
            var cleaned = sensorsString.Trim().Trim('"', '\'');
            if (cleaned.Contains(','))
            {
                return cleaned.Split(',')
                    .Select(s => s.Trim().Trim('"', '\''))
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToList();
            }

            var trimmed = sensorsString.Trim('"', '\'').Trim();
            return string.IsNullOrWhiteSpace(trimmed) ? new List<string>() : new List<string> { trimmed };
        }
    }
}
