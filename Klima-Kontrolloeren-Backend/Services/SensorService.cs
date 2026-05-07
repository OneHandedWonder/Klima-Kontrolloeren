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
}