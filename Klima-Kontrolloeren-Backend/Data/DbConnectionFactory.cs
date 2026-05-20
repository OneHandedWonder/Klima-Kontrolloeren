using Microsoft.Data.SqlClient;
using KlimaKontrolloerenBackend.Models;
using KlimaKontrolloerenBackend.Data;
using System.Diagnostics.CodeAnalysis;

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

    [ExcludeFromCodeCoverage(Justification = "Direct SQL execution is covered by integration tests, not unit tests.")]
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

    [ExcludeFromCodeCoverage(Justification = "Direct SQL execution is covered by integration tests, not unit tests.")]
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

    [ExcludeFromCodeCoverage(Justification = "Direct SQL execution is covered by integration tests, not unit tests.")]
    public async Task<List<SensorReading>> GetReadingsAsync(int limit, string uid)
    {
        // First, get the user's sensors from KlimaDataUsers
        const string userSql = """
            SELECT Sensors
            FROM KlimaDataUsers
            WHERE UID = @FirebaseUID
            """;

        var userSensors = new List<string>();

        await using var conn = new SqlConnection(_connectionString);
        await using var cmd = new SqlCommand(userSql, conn);
        cmd.Parameters.AddWithValue("@FirebaseUID", uid);

        await conn.OpenAsync();
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var sensorsString = reader.GetString(0);
            // Parse sensors array
            userSensors = ParseSensors(sensorsString);
        }

        if (userSensors.Count == 0)
            return new List<SensorReading>();

        // Now query readings for only those sensors
        var sensorIdList = string.Join("','", userSensors.Select(s => s.Replace("'", "''")));
        var readingsSql = $"""
            SELECT TOP (@Limit) Id, FirebaseUID AS SourceId, SensorId, Temperature, Humidity, CO2PPM, RecordedAt
            FROM SensorReadings
            WHERE SensorId IN ('{sensorIdList}')
            ORDER BY RecordedAt DESC
            """;

        var readings = new List<SensorReading>();

        await using var conn2 = new SqlConnection(_connectionString);
        await using var cmd2 = new SqlCommand(readingsSql, conn2);
        cmd2.Parameters.AddWithValue("@Limit", limit);

        await conn2.OpenAsync();
        await using var reader2 = await cmd2.ExecuteReaderAsync();

        while (await reader2.ReadAsync())
        {
            readings.Add(new SensorReading
            {
                Id = reader2.GetInt32(0),
                SourceId = reader2.GetString(1),
                SensorId = reader2.GetString(2),
                Temperature = reader2.GetDouble(3),
                Humidity = reader2.GetDouble(4),
                CO2PPM = reader2.GetDouble(5),
                RecordedAt = reader2.GetDateTime(6)
            });
        }

        return readings;
    }

    private List<string> ParseSensors(string sensorsString)
    {
        if (string.IsNullOrWhiteSpace(sensorsString))
            return new List<string>();

        try
        {
            // Try to parse as JSON array first
            var parsed = System.Text.Json.JsonSerializer.Deserialize<string[]>(sensorsString);
            return parsed?.ToList() ?? new List<string>();
        }
        catch
        {
            // If it's comma-separated with quotes like: "pi-sensor-01", "pi-sensor-02"
            var cleaned = sensorsString.Trim().Trim('"', '\'');
            if (cleaned.Contains(','))
            {
                return cleaned.Split(',')
                    .Select(s => s.Trim().Trim('"', '\''))
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToList();
            }
            
            // Single sensor
            var trimmed = sensorsString.Trim('"', '\'').Trim();
            return string.IsNullOrWhiteSpace(trimmed) ? new List<string>() : new List<string> { trimmed };
        }
    }
    [ExcludeFromCodeCoverage(Justification = "Direct SQL execution is covered by integration tests, not unit tests.")]
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

    [ExcludeFromCodeCoverage(Justification = "Direct SQL execution is covered by integration tests, not unit tests.")]
    public async Task<List<SensorReading>> GetDaylyReadingsAsync(string uid)
    {
        // First, get the user's sensors
        var userSensors = await GetUserSensorsListAsync(uid);
        if (userSensors.Count == 0)
            return new List<SensorReading>();

        var sensorIdList = string.Join("','", userSensors.Select(s => s.Replace("'", "''")));
        var sql = $"""
            SELECT Id, FirebaseUID AS SourceId, SensorId, Temperature, Humidity, CO2PPM, RecordedAt
            FROM SensorReadings
            WHERE SensorId IN ('{sensorIdList}') AND RecordedAt >= DATEADD(day, -1, GETDATE())
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
    [ExcludeFromCodeCoverage(Justification = "Direct SQL execution is covered by integration tests, not unit tests.")]
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

    [ExcludeFromCodeCoverage(Justification = "Direct SQL execution is covered by integration tests, not unit tests.")]
    public async Task<List<SensorReading>> GetWeeklyReadingsAsync(string uid)
    {
        var userSensors = await GetUserSensorsListAsync(uid);
        if (userSensors.Count == 0)
            return new List<SensorReading>();

        var sensorIdList = string.Join("','", userSensors.Select(s => s.Replace("'", "''")));
        var sql = $"""
            SELECT Id, FirebaseUID AS SourceId, SensorId, Temperature, Humidity, CO2PPM, RecordedAt
            FROM SensorReadings
            WHERE SensorId IN ('{sensorIdList}') AND RecordedAt >= DATEADD(week, -1, GETDATE())
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

    [ExcludeFromCodeCoverage(Justification = "Direct SQL execution is covered by integration tests, not unit tests.")]
    public async Task<List<AverageData>> GetWeeklyAveragesAsync()
    {
        const string sql = """
            SELECT MyDate, Temperature, Humidity, CO2PPM
            FROM Day7AVG
            ORDER BY MyDate ASC
            """;

        var averages = new List<AverageData>();

        await using var conn = new SqlConnection(_connectionString);
        await using var cmd = new SqlCommand(sql, conn);

        await conn.OpenAsync();
        await using var reader = await cmd.ExecuteReaderAsync();

        var index = 0;
        while (await reader.ReadAsync())
        {
            averages.Add(new AverageData
            {
                TimePeriod = reader.GetDateTime(0),
                ReadingCount = ++index,
                AverageTemperature = Convert.ToDouble(reader.GetValue(1)),
                AverageHumidity = Convert.ToDouble(reader.GetValue(2)),
                AverageCO2PPM = Convert.ToDouble(reader.GetValue(3))
            });
        }

        return averages;
    }

    [ExcludeFromCodeCoverage(Justification = "Direct SQL execution is covered by integration tests, not unit tests.")]
    public async Task<List<AverageData>> GetMonthlyAveragesAsync()
    {
        const string sql = """
            SELECT MyDate, Temperature, Humidity, CO2PPM
            FROM Day30AVG
            ORDER BY MyDate ASC
            """;

        var averages = new List<AverageData>();

        await using var conn = new SqlConnection(_connectionString);
        await using var cmd = new SqlCommand(sql, conn);

        await conn.OpenAsync();
        await using var reader = await cmd.ExecuteReaderAsync();

        var index = 0;
        while (await reader.ReadAsync())
        {
            averages.Add(new AverageData
            {
                TimePeriod = reader.GetDateTime(0),
                ReadingCount = ++index,
                AverageTemperature = Convert.ToDouble(reader.GetValue(1)),
                AverageHumidity = Convert.ToDouble(reader.GetValue(2)),
                AverageCO2PPM = Convert.ToDouble(reader.GetValue(3))
            });
        }

        return averages;
    }

    [ExcludeFromCodeCoverage(Justification = "Direct SQL execution is covered by integration tests, not unit tests.")]
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

    [ExcludeFromCodeCoverage(Justification = "Direct SQL execution is covered by integration tests, not unit tests.")]
    public async Task<List<SensorReading>> GetMonthlyReadingsAsync(string uid)
    {
        var userSensors = await GetUserSensorsListAsync(uid);
        if (userSensors.Count == 0)
            return new List<SensorReading>();

        var sensorIdList = string.Join("','", userSensors.Select(s => s.Replace("'", "''")));
        var sql = $"""
            SELECT Id, FirebaseUID AS SourceId, SensorId, Temperature, Humidity, CO2PPM, RecordedAt
            FROM SensorReadings
            WHERE SensorId IN ('{sensorIdList}') AND RecordedAt >= DATEADD(month, -1, GETDATE())
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

    [ExcludeFromCodeCoverage(Justification = "Direct SQL execution is covered by integration tests, not unit tests.")]
    private async Task<List<string>> GetUserSensorsListAsync(string uid)
    {
        const string sql = """
            SELECT Sensors
            FROM KlimaDataUsers
            WHERE UID = @FirebaseUID
            """;

        await using var conn = new SqlConnection(_connectionString);
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@FirebaseUID", uid);

        await conn.OpenAsync();
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var sensorsString = reader.GetString(0);
            return ParseSensors(sensorsString);
        }

        return new List<string>();
    }
    [ExcludeFromCodeCoverage(Justification = "Direct SQL execution is covered by integration tests, not unit tests.")]
    public async Task<List<KlimaDataUser>> GetKlimaDataUserAsync(string firebaseUid)
    {
        const string sql = """
            SELECT UID, Sensors, Enabled
            FROM KlimaDataUsers
            WHERE UID = @FirebaseUID
            """;

        await using var conn = new SqlConnection(_connectionString);
        await using var cmd = new SqlCommand(sql, conn);

        cmd.Parameters.AddWithValue("@FirebaseUID", firebaseUid);

        await conn.OpenAsync();
        await using var reader = await cmd.ExecuteReaderAsync();

        var users = new List<KlimaDataUser>();

        while (await reader.ReadAsync())
        {
            users.Add(new KlimaDataUser
            {
                Uid = reader.GetString(0),
                Sensors = reader.GetString(1),
                Enabled = reader.GetBoolean(2)
            });
        }

        return users;
    }

    [ExcludeFromCodeCoverage(Justification = "Placeholder for database-backed UID lookup; actual token lookup is handled by AuthService.")]
    public async Task<List<string>> GetUserUIDAsync(string token)
    {
        // For now, return empty list or throw not implemented
        // This is a placeholder - the actual UID lookup is handled in AuthService
        await Task.CompletedTask;
        return new List<string>();
    }
}
