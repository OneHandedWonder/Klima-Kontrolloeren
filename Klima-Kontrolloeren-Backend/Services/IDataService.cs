using KlimaKontrolloerenBackend.Models;

namespace KlimaKontrolloerenBackend.Services;

public interface IDataService
{
    Task<List<SensorReading>> GetReadingsAsync(int limit = 80640);
}