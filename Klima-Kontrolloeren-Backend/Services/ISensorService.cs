using KlimaKontrolloerenBackend.Models;

namespace KlimaKontrolloerenBackend.Services;

public interface ISensorService
{
    Task SaveReadingAsync(SensorReadingDto dto);
    Task<List<SensorReading>> GetReadingsAsync(int limit = 100);
    Task<List<SensorReading>> GetReadingsAsync(int limit, string uid);
    Task<List<SensorReading>> GetDaylyReadingsAsync();
    Task<List<SensorReading>> GetDaylyReadingsAsync(string uid);
    Task<List<SensorReading>> GetWeeklyReadingsAsync();
    Task<List<SensorReading>> GetWeeklyReadingsAsync(string uid);
    Task<List<SensorReading>> GetMonthlyReadingsAsync();
    Task<List<SensorReading>> GetMonthlyReadingsAsync(string uid);
    Task<List<KlimaDataUser>> GetUserSensorsAsync(string uid);
}