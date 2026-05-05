using KlimaKontrolloerenBackend.Models;

namespace KlimaKontrolloerenBackend.Data;

public interface IDbConnectionFactory
{
    Task SaveReadingAsync(string firebaseUid, SensorReadingDto dto);
    Task<List<SensorReading>> GetReadingsAsync(string firebaseUid, int limit);
}