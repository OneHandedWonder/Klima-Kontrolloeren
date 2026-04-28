public class WeatherReport
{
    public string City { get; set; } = string.Empty;
    public double TemperatureCurrent { get; set; }
    public string Condition { get; set; } = string.Empty;
    public string warnings { get; set; } = string.Empty;
    public string humidity { get; set; } = string.Empty;
    public Dictionary<string, float> Temperatures { get; set; } = new();
    public Dictionary<string, float> HourlyRainChance { get; set; } = new();
    public Dictionary<string, WeatherStation> Stations { get; set; } = new();
}

public class WeatherStation
{
    public double Distance { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int UseCount { get; set; }
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Quality { get; set; }
    public double Contribution { get; set; }
}