using KlimaKontrolloerenBackend.Models;

namespace KlimaKontrolloerenBackend.Data;

public interface IDbConnectionFactory
{
    Task SaveReadingAsync(SensorReadingDto dto);
    Task<List<SensorReading>> GetReadingsAsync(int limit);
}