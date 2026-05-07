namespace KlimaKontrolloerenBackend.Models;

public class SensorReading
{
    public int Id { get; set; }
    public string SourceId { get; set; } = string.Empty;
    public string SensorId { get; set; } = string.Empty;
    public double Temperature { get; set; }   // °C
    public double Humidity { get; set; }      // %
    public double CO2PPM { get; set; }        // PPM
    public DateTime RecordedAt { get; set; }
}

public class SensorReadingDto
{
    public string SensorId { get; set; } = string.Empty;
    public double Temperature { get; set; }
    public double Humidity { get; set; }
    public double CO2PPM { get; set; }
}