namespace KlimaKontrolloerenBackend.Models;
public class AverageData
{
    public DateTime TimePeriod { get; set; }
    public int ReadingCount { get; set; }
    public double? AverageTemperature { get; set; }
    public double? AverageHumidity { get; set; }
    public double? AverageCO2PPM { get; set; }
}