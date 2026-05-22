using System.Globalization;
using System.Text;
using KlimaKontrolloerenBackend.Models;

namespace KlimaKontrolloerenBackend.Services;

public interface ICsvService
{
    string GenerateSensorReadingsCsv(IEnumerable<SensorReading> readings);
}

public class CsvService : ICsvService
{
    public string GenerateSensorReadingsCsv(IEnumerable<SensorReading> readings)
    {
        ArgumentNullException.ThrowIfNull(readings);

        var csv = new StringBuilder();
        csv.AppendLine("Id,SourceId,SensorId,Temperature,Humidity,CO2PPM,RecordedAt");

        foreach (var reading in readings)
        {
            csv.Append(reading.Id.ToString(CultureInfo.InvariantCulture));
            csv.Append(',');
            csv.Append(Escape(reading.SourceId));
            csv.Append(',');
            csv.Append(Escape(reading.SensorId));
            csv.Append(',');
            csv.Append(reading.Temperature.ToString(CultureInfo.InvariantCulture));
            csv.Append(',');
            csv.Append(reading.Humidity.ToString(CultureInfo.InvariantCulture));
            csv.Append(',');
            csv.Append(reading.CO2PPM.ToString(CultureInfo.InvariantCulture));
            csv.Append(',');
            csv.Append(Escape(reading.RecordedAt.ToString("O", CultureInfo.InvariantCulture)));
            csv.AppendLine();
        }

        return csv.ToString();
    }

    private static string Escape(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        var needsQuotes = value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r');
        if (!needsQuotes)
            return value;

        return $"\"{value.Replace("\"", "\"\"")}\"";
    }
}
