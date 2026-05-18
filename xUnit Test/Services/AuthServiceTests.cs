using System.Net;
using System.Text;
using KlimaKontrolloerenBackend.Data;
using KlimaKontrolloerenBackend.Models;
using KlimaKontrolloerenBackend.Services;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace KlimaKontrolloerenBackend.Tests.Services;

public class AuthServiceTests
{
    private readonly Mock<IDbConnectionFactory> _db = new();

    [Theory]
    [InlineData("", "password")]
    [InlineData("test@example.com", "")]
    public async Task SignInAsync_MissingEmailOrPassword_ReturnsNull(string email, string password)
    {
        var sut = CreateService("""{"idToken":"token"}""");

        var result = await sut.SignInAsync(email, password);

        Assert.Null(result);
    }

    [Fact]
    public async Task SignInAsync_MissingFirebaseApiKey_ThrowsInvalidOperationException()
    {
        var sut = CreateService("""{}""", firebaseApiKey: null);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.SignInAsync("test@example.com", "password"));
    }

    [Fact]
    public async Task SignInAsync_FirebaseRejectsCredentials_ReturnsNull()
    {
        var sut = CreateService("""{}""", HttpStatusCode.BadRequest);

        var result = await sut.SignInAsync("test@example.com", "wrong-password");

        Assert.Null(result);
    }

    [Fact]
    public async Task SignInAsync_UserIsMissingFromDatabase_ReturnsNull()
    {
        var sut = CreateService("""
            {
              "email": "test@example.com",
              "localId": "uid-1",
              "idToken": "id-token",
              "refreshToken": "refresh-token",
              "expiresIn": "3600"
            }
            """);

        _db.Setup(x => x.GetKlimaDataUserAsync("uid-1")).ReturnsAsync(new List<KlimaDataUser>());

        var result = await sut.SignInAsync("test@example.com", "password");

        Assert.Null(result);
    }

    [Fact]
    public async Task SignInAsync_DisabledUser_ReturnsNull()
    {
        var sut = CreateService("""
            {
              "email": "test@example.com",
              "localId": "uid-1",
              "idToken": "id-token",
              "refreshToken": "refresh-token",
              "expiresIn": "3600"
            }
            """);

        _db.Setup(x => x.GetKlimaDataUserAsync("uid-1")).ReturnsAsync(new List<KlimaDataUser>
        {
            new() { Uid = "uid-1", Enabled = false }
        });

        var result = await sut.SignInAsync("test@example.com", "password");

        Assert.Null(result);
    }

    [Fact]
    public async Task SignInAsync_EnabledUser_ReturnsAuthResult()
    {
        var sut = CreateService("""
            {
              "email": "test@example.com",
              "localId": "uid-1",
              "idToken": "id-token",
              "refreshToken": "refresh-token",
              "expiresIn": "3600"
            }
            """);

        _db.Setup(x => x.GetKlimaDataUserAsync("uid-1")).ReturnsAsync(new List<KlimaDataUser>
        {
            new() { Uid = "uid-1", Enabled = true }
        });

        var result = await sut.SignInAsync("test@example.com", "password");

        Assert.NotNull(result);
        Assert.Equal("id-token", result.IdToken);
        Assert.Equal("refresh-token", result.RefreshToken);
        Assert.Equal(3600, result.ExpiresIn);
    }

    [Fact]
    public async Task SignInAsync_WithNonNumericExpiresIn_ReturnsZeroExpiresIn()
    {
        var sut = CreateService("""
            {
              "email": "test@example.com",
              "localId": "uid-1",
              "idToken": "id-token",
              "refreshToken": "refresh-token",
              "expiresIn": "not-a-number"
            }
            """);

        _db.Setup(x => x.GetKlimaDataUserAsync("uid-1")).ReturnsAsync(new List<KlimaDataUser>
        {
            new() { Uid = "uid-1", Enabled = true }
        });

        var result = await sut.SignInAsync("test@example.com", "password");

        Assert.NotNull(result);
        Assert.Equal(0, result.ExpiresIn);
    }

    [Fact]
    public async Task GetUserSensorsAsync_MissingToken_ReturnsNull()
    {
        var sut = CreateService("""{}""");

        var result = await sut.GetUserSensorsAsync("");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetUserSensorsAsync_ValidToken_ReturnsDatabaseUsers()
    {
        var expected = new List<KlimaDataUser> { new() { Uid = "uid-1", Enabled = true } };
        _db.Setup(x => x.GetKlimaDataUserAsync("uid-1")).ReturnsAsync(expected);
        var sut = CreateService("""{}""");

        var result = await sut.GetUserSensorsAsync("uid-1");

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task GetUserUIDAsync_MissingToken_ReturnsNull()
    {
        var sut = CreateService("""{}""");

        var result = await sut.GetUserUIDAsync("");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetUserUIDAsync_MissingFirebaseApiKey_ReturnsNull()
    {
        var sut = CreateService("""{}""", firebaseApiKey: null);

        var result = await sut.GetUserUIDAsync("id-token");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetUserUIDAsync_FirebaseLookupFails_ReturnsNull()
    {
        var sut = CreateService("""{}""", HttpStatusCode.BadRequest);

        var result = await sut.GetUserUIDAsync("id-token");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetUserUIDAsync_FirebaseLookupSucceeds_ReturnsLocalId()
    {
        var sut = CreateService("""
            {
              "users": [
                { "localId": "uid-1" }
              ]
            }
            """);

        var result = await sut.GetUserUIDAsync("id-token");

        Assert.Equal("uid-1", result);
    }

    [Fact]
    public async Task GetUserUIDAsync_FirebaseLookupReturnsNoUsers_ReturnsNull()
    {
        var sut = CreateService("""{ "users": [] }""");

        var result = await sut.GetUserUIDAsync("id-token");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetUserUIDAsync_WhenHttpClientThrows_ReturnsNull()
    {
        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory
            .Setup(x => x.CreateClient(nameof(AuthService)))
            .Returns(new HttpClient(new ThrowingHttpMessageHandler()));

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Firebase:ApiKey"] = "firebase-api-key"
            })
            .Build();

        var sut = new AuthService(httpClientFactory.Object, configuration, _db.Object);

        var result = await sut.GetUserUIDAsync("id-token");

        Assert.Null(result);
    }

    private AuthService CreateService(
        string responseBody,
        HttpStatusCode statusCode = HttpStatusCode.OK,
        string? firebaseApiKey = "firebase-api-key")
    {
        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory
            .Setup(x => x.CreateClient(nameof(AuthService)))
            .Returns(new HttpClient(new StubHttpMessageHandler(statusCode, responseBody)));

        var configValues = new Dictionary<string, string?>();
        if (firebaseApiKey is not null)
            configValues["Firebase:ApiKey"] = firebaseApiKey;

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues)
            .Build();

        return new AuthService(httpClientFactory.Object, configuration, _db.Object);
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string _responseBody;

        public StubHttpMessageHandler(HttpStatusCode statusCode, string responseBody)
        {
            _statusCode = statusCode;
            _responseBody = responseBody;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_responseBody, Encoding.UTF8, "application/json")
            };

            return Task.FromResult(response);
        }
    }

    private sealed class ThrowingHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            throw new HttpRequestException("Request failed.");
        }
    }
}
