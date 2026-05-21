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
    Task<List<AverageData>> GetWeeklyAveragesAsync();
    Task<List<AverageData>> GetMonthlyAveragesAsync();
    Task<List<SensorReading>> GetMonthlyReadingsAsync();
    Task<List<SensorReading>> GetMonthlyReadingsAsync(string uid);
    Task<List<KlimaDataUser>> GetUserSensorsAsync(string uid);
    Task<SensorInfoResult?> GetSensorInfoAsync(string uid, string sensorId);
    Task UpdateSensorInfoAsync(string uid, string sensorId, string name, string type, string location);
    Task AddSensorToUserAsync(string uid, string sensorId);
    Task RemoveSensorFromUserAsync(string uid, string sensorId);
}