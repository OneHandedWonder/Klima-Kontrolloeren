using KlimaKontrolloerenBackend.Data;
using Microsoft.Extensions.Configuration;
using System.Reflection;
using Xunit;

namespace KlimaKontrolloerenBackend.Tests.Data;

public class DbConnectionFactoryTests
{
    [Fact]
    public void Constructor_MissingDefaultConnection_ThrowsInvalidOperationException()
    {
        var configuration = new ConfigurationBuilder().Build();

        Assert.Throws<InvalidOperationException>(() => new DbConnectionFactory(configuration));
    }

    [Fact]
    public void Constructor_WithDefaultConnection_CreatesFactory()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=localhost;Database=Klima;User Id=test;Password=test;TrustServerCertificate=True"
            })
            .Build();

        var factory = new DbConnectionFactory(configuration);

        Assert.NotNull(factory);
    }

    [Theory]
    [InlineData("[\"pi-sensor-01\",\"pi-sensor-02\"]", "pi-sensor-01", "pi-sensor-02")]
    [InlineData("\"pi-sensor-01\", \"pi-sensor-02\"", "pi-sensor-01", "pi-sensor-02")]
    [InlineData("\"pi-sensor-01\"", "pi-sensor-01")]
    public void ParseSensors_WithSupportedFormats_ReturnsSensorIds(string storedSensors, params string[] expected)
    {
        var factory = CreateFactory();

        var result = InvokeParseSensors(factory, storedSensors);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\"\"")]
    public void ParseSensors_WithBlankInput_ReturnsEmptyList(string storedSensors)
    {
        var factory = CreateFactory();

        var result = InvokeParseSensors(factory, storedSensors);

        Assert.Empty(result);
    }

    private static DbConnectionFactory CreateFactory()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=localhost;Database=Klima;User Id=test;Password=test;TrustServerCertificate=True"
            })
            .Build();

        return new DbConnectionFactory(configuration);
    }

    private static List<string> InvokeParseSensors(DbConnectionFactory factory, string storedSensors)
    {
        var method = typeof(DbConnectionFactory).GetMethod("ParseSensors", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException(nameof(DbConnectionFactory), "ParseSensors");

        return Assert.IsType<List<string>>(method.Invoke(factory, new object[] { storedSensors }));
    }
}
