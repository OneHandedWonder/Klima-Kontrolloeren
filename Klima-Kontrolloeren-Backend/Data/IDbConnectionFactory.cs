using KlimaKontrolloerenBackend.Models;

namespace KlimaKontrolloerenBackend.Data;

public interface IDbConnectionFactory
{
    Task SaveReadingAsync(SensorReadingDto dto);
    Task<List<SensorReading>> GetReadingsAsync(int limit);
    Task<List<SensorReading>> GetReadingsAsync(int limit, string uid);
    Task<List<KlimaDataUser>> GetKlimaDataUserAsync(string firebaseUid);
    Task<List<string>> GetUserUIDAsync(string token);
    Task<List<SensorReading>> GetDaylyReadingsAsync();
    Task<List<SensorReading>> GetDaylyReadingsAsync(string uid);
    Task<List<SensorReading>> GetWeeklyReadingsAsync();
    Task<List<SensorReading>> GetWeeklyReadingsAsync(string uid);
    Task<List<AverageData>> GetWeeklyAveragesAsync();
    Task<List<AverageData>> GetMonthlyAveragesAsync();
    Task<List<SensorReading>> GetMonthlyReadingsAsync();
    Task<List<SensorReading>> GetMonthlyReadingsAsync(string uid);
}