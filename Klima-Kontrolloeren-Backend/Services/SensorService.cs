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
    public async Task<List<SensorReading>> GetDaylyReadingsAsync()
    {
        return await _db.GetDaylyReadingsAsync();
    }

    public async Task<List<SensorReading>> GetWeeklyReadingsAsync()
    {
        return await _db.GetWeeklyReadingsAsync();
    }
    public async Task<List<SensorReading>> GetMonthlyReadingsAsync()
    {
        return await _db.GetMonthlyReadingsAsync();
    }
}