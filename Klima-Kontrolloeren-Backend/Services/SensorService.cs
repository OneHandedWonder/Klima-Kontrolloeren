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
}