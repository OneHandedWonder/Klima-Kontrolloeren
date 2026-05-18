using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace KlimaKontrolloerenBackend.Tests.Integration;

public class BackendStartupTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public BackendStartupTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task RootEndpoint_ReturnsBackendIsRunningMessage()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/");
        var body = await response.Content.ReadAsStringAsync();

        response.EnsureSuccessStatusCode();
        Assert.Contains("Klima-Kontrolloeren backend is running.", body);
    }
}
