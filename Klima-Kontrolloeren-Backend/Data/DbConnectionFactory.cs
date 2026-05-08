using Microsoft.Data.SqlClient;
using KlimaKontrolloerenBackend.Models;
using KlimaKontrolloerenBackend.Data;

namespace KlimaKontrolloerenBackend.Data;

// DbConnectionFactory implements IDbConnectionFactory (now defined in IDbConnectionFactory.cs)
public class DbConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public DbConnectionFactory(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");
    }

    public async Task SaveReadingAsync(SensorReadingDto dto)
    {
        const string sql = """
            INSERT INTO SensorReadings (FirebaseUID, SensorId, Temperature, Humidity, CO2PPM)
            VALUES (@FirebaseUID, @SensorId, @Temperature, @Humidity, @CO2PPM)
            """;

        await using var conn = new SqlConnection(_connectionString);
        await using var cmd = new SqlCommand(sql, conn);

        cmd.Parameters.AddWithValue("@FirebaseUID", dto.SensorId);
        cmd.Parameters.AddWithValue("@SensorId", dto.SensorId);
        cmd.Parameters.AddWithValue("@Temperature", dto.Temperature);
        cmd.Parameters.AddWithValue("@Humidity", dto.Humidity);
        cmd.Parameters.AddWithValue("@CO2PPM", dto.CO2PPM);

        await conn.OpenAsync();
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<List<SensorReading>> GetReadingsAsync(int limit)
    {
    const string sql = """
        SELECT TOP (@Limit) Id, FirebaseUID AS SourceId, SensorId, Temperature, Humidity, CO2PPM, RecordedAt
        FROM SensorReadings
        ORDER BY RecordedAt DESC
        """;

    var readings = new List<SensorReading>();

    await using var conn = new SqlConnection(_connectionString);
    await using var cmd = new SqlCommand(sql, conn);

    cmd.Parameters.AddWithValue("@Limit", limit);

    await conn.OpenAsync();
    await using var reader = await cmd.ExecuteReaderAsync();

    while (await reader.ReadAsync())
    {
        readings.Add(new SensorReading
        {
            Id = reader.GetInt32(0),
            SourceId = reader.GetString(1),
            SensorId = reader.GetString(2),
            Temperature = reader.GetDouble(3),
            Humidity = reader.GetDouble(4),
            CO2PPM = reader.GetDouble(5),
            RecordedAt = reader.GetDateTime(6)
        });
    }

    return readings;
}
public async Task<List<SensorReading>> GetDaylyReadingsAsync()
    {
        const string sql = """
            SELECT Id, FirebaseUID AS SourceId, SensorId, Temperature, Humidity, CO2PPM, RecordedAt
            FROM SensorReadings
            WHERE RecordedAt >= DATEADD(day, -1, GETDATE())
            ORDER BY RecordedAt ASC
            """;

        var readings = new List<SensorReading>();

        await using var conn = new SqlConnection(_connectionString);
        await using var cmd = new SqlCommand(sql, conn);

        await conn.OpenAsync();
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            readings.Add(new SensorReading
            {
                Id = reader.GetInt32(0),
                SourceId = reader.GetString(1),
                SensorId = reader.GetString(2),
                Temperature = reader.GetDouble(3),
                Humidity = reader.GetDouble(4),
                CO2PPM = reader.GetDouble(5),
                RecordedAt = reader.GetDateTime(6)
            });
        }

        return readings;
    }
    public async Task<List<SensorReading>> GetWeeklyReadingsAsync()
    {
        const string sql = """
            SELECT Id, FirebaseUID AS SourceId, SensorId, Temperature, Humidity, CO2PPM, RecordedAt
            FROM SensorReadings
            WHERE RecordedAt >= DATEADD(week, -1, GETDATE())
            ORDER BY RecordedAt ASC
            """;

        var readings = new List<SensorReading>();

        await using var conn = new SqlConnection(_connectionString);
        await using var cmd = new SqlCommand(sql, conn);

        await conn.OpenAsync();
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            readings.Add(new SensorReading
            {
                Id = reader.GetInt32(0),
                SourceId = reader.GetString(1),
                SensorId = reader.GetString(2),
                Temperature = reader.GetDouble(3),
                Humidity = reader.GetDouble(4),
                CO2PPM = reader.GetDouble(5),
                RecordedAt = reader.GetDateTime(6)
            });
        }

        return readings;
    }
    public async Task<List<SensorReading>> GetMonthlyReadingsAsync()
    {
        const string sql = """
            SELECT Id, FirebaseUID AS SourceId, SensorId, Temperature, Humidity, CO2PPM, RecordedAt
            FROM SensorReadings
            WHERE RecordedAt >= DATEADD(month, -1, GETDATE())
            ORDER BY RecordedAt ASC
            """;

        var readings = new List<SensorReading>();

        await using var conn = new SqlConnection(_connectionString);
        await using var cmd = new SqlCommand(sql, conn);

        await conn.OpenAsync();
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            readings.Add(new SensorReading
            {
                Id = reader.GetInt32(0),
                SourceId = reader.GetString(1),
                SensorId = reader.GetString(2),
                Temperature = reader.GetDouble(3),
                Humidity = reader.GetDouble(4),
                CO2PPM = reader.GetDouble(5),
                RecordedAt = reader.GetDateTime(6)
            });
        }

        return readings;
    }
}