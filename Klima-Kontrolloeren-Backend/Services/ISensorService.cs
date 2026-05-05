using KlimaKontrolloerenBackend.Models;

namespace KlimaKontrolloerenBackend.Services;

public interface ISensorService
{
    Task SaveReadingAsync(string firebaseUid, SensorReadingDto dto);
    Task<List<SensorReading>> GetReadingsAsync(string firebaseUid, int limit = 100);
}