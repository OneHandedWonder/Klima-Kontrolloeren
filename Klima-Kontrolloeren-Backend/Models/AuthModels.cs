using System.Text.Json.Serialization;

namespace KlimaKontrolloerenBackend.Models;

public sealed class SignInRequest
{
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;
}

public sealed class AuthResult
{
    [JsonPropertyName("uid")]
    public string Uid { get; set; } = string.Empty;

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    [JsonPropertyName("sensors")]
    public string Sensors { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [JsonPropertyName("idToken")]
    public string IdToken { get; set; } = string.Empty;

    [JsonPropertyName("refreshToken")]
    public string RefreshToken { get; set; } = string.Empty;

    [JsonPropertyName("expiresIn")]
    public int ExpiresIn { get; set; }
}

public sealed class KlimaDataUser
{
    [JsonPropertyName("uid")]
    public string Uid { get; set; } = string.Empty;

    [JsonPropertyName("sensors")]
    public string Sensors { get; set; } = string.Empty;

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }
}

// Sensor management models
public sealed class SensorInfoResult
{
    [JsonPropertyName("sensorId")]
    public string SensorId { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("location")]
    public string Location { get; set; } = string.Empty;
}

public sealed class UpdateSensorInfoRequest
{
    [JsonPropertyName("uid")]
    public string Uid { get; set; } = string.Empty;

    [JsonPropertyName("sensorId")]
    public string SensorId { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("location")]
    public string Location { get; set; } = string.Empty;
}

public sealed class AddSensorRequest
{
    [JsonPropertyName("uid")]
    public string Uid { get; set; } = string.Empty;

    [JsonPropertyName("sensorId")]
    public string SensorId { get; set; } = string.Empty;
}