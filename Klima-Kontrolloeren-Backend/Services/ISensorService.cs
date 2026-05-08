using KlimaKontrolloerenBackend.Models;

namespace KlimaKontrolloerenBackend.Services;

public interface ISensorService
{
    Task SaveReadingAsync(SensorReadingDto dto);
    Task<List<SensorReading>> GetReadingsAsync(int limit = 100);
    Task<List<SensorReading>> GetDaylyReadingsAsync();
    Task<List<SensorReading>> GetWeeklyReadingsAsync();
    Task<List<SensorReading>> GetMonthlyReadingsAsync();
}