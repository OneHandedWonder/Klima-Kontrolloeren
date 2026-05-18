using System.Text.Json;
using KlimaKontrolloerenBackend.Models;
using Xunit;

namespace KlimaKontrolloerenBackend.Tests.Models;

public class AuthModelsTests
{
    [Fact]
    public void SignInRequest_DefaultValues_AreEmptyStrings()
    {
        var model = new SignInRequest();

        Assert.Equal(string.Empty, model.Email);
        Assert.Equal(string.Empty, model.Password);
    }

    [Fact]
    public void SignInRequest_DeserializesJsonPropertyNames()
    {
        var model = JsonSerializer.Deserialize<SignInRequest>("""
            {
              "email": "test@example.com",
              "password": "secret123"
            }
            """);

        Assert.NotNull(model);
        Assert.Equal("test@example.com", model.Email);
        Assert.Equal("secret123", model.Password);
    }

    [Fact]
    public void AuthResult_SerializesExpectedJsonPropertyNames()
    {
        var model = new AuthResult
        {
            Uid = "uid-1",
            Enabled = true,
            Sensors = "[\"pi-sensor-01\"]",
            Email = "test@example.com",
            IdToken = "id-token",
            RefreshToken = "refresh-token",
            ExpiresIn = 3600
        };

        var json = JsonSerializer.Serialize(model);
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        Assert.Equal("uid-1", root.GetProperty("uid").GetString());
        Assert.True(root.GetProperty("enabled").GetBoolean());
        Assert.Equal("[\"pi-sensor-01\"]", root.GetProperty("sensors").GetString());
        Assert.Equal("test@example.com", root.GetProperty("email").GetString());
        Assert.Equal("id-token", root.GetProperty("idToken").GetString());
        Assert.Equal("refresh-token", root.GetProperty("refreshToken").GetString());
        Assert.Equal(3600, root.GetProperty("expiresIn").GetInt32());
    }

    [Fact]
    public void KlimaDataUser_DeserializesJsonPropertyNames()
    {
        var model = JsonSerializer.Deserialize<KlimaDataUser>("""
            {
              "uid": "uid-1",
              "sensors": "[\"pi-sensor-01\"]",
              "enabled": true
            }
            """);

        Assert.NotNull(model);
        Assert.Equal("uid-1", model.Uid);
        Assert.Equal("[\"pi-sensor-01\"]", model.Sensors);
        Assert.True(model.Enabled);
    }
}
