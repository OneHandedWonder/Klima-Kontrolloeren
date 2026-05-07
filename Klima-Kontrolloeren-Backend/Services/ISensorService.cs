using KlimaKontrolloerenBackend.Models;

namespace KlimaKontrolloerenBackend.Services;

public interface ISensorService
{
    Task SaveReadingAsync(SensorReadingDto dto);
    Task<List<SensorReading>> GetReadingsAsync(int limit = 100);
}