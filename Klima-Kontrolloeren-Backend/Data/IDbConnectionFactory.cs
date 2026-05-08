using KlimaKontrolloerenBackend.Models;

namespace KlimaKontrolloerenBackend.Data;

public interface IDbConnectionFactory
{
    Task SaveReadingAsync(SensorReadingDto dto);
    Task<List<SensorReading>> GetReadingsAsync(int limit);
    Task<List<SensorReading>> GetDaylyReadingsAsync();
    Task<List<SensorReading>> GetWeeklyReadingsAsync();
    Task<List<SensorReading>> GetMonthlyReadingsAsync();
}