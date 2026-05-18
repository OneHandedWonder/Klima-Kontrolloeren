using KlimaKontrolloerenBackend.Controllers;
using KlimaKontrolloerenBackend.Models;
using KlimaKontrolloerenBackend.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace KlimaKontrolloerenBackend.Tests.Controllers;

public class AuthControllerTests
{
    private readonly Mock<IAuthService> _authService = new();
    private readonly AuthController _sut;

    public AuthControllerTests()
    {
        _sut = new AuthController(_authService.Object);
    }

    [Fact]
    public async Task SignIn_MissingEmailOrPassword_Returns400()
    {
        var result = await _sut.SignIn(new SignInRequest { Email = "", Password = "" });

        Assert.IsType<BadRequestObjectResult>(result);
        _authService.Verify(x => x.SignInAsync(It.IsAny<string>(), It.IsAny<string>(), default), Times.Never);
    }

    [Fact]
    public async Task SignIn_InvalidCredentials_Returns401()
    {
        _authService
            .Setup(x => x.SignInAsync("test@example.com", "wrong-password", default))
            .ReturnsAsync((AuthResult?)null);

        var result = await _sut.SignIn(new SignInRequest
        {
            Email = "test@example.com",
            Password = "wrong-password"
        });

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task SignIn_ValidCredentials_ReturnsAuthResult()
    {
        var authResult = new AuthResult
        {
            IdToken = "id-token",
            RefreshToken = "refresh-token",
            ExpiresIn = 3600
        };

        _authService
            .Setup(x => x.SignInAsync("test@example.com", "correct-password", default))
            .ReturnsAsync(authResult);

        var result = await _sut.SignIn(new SignInRequest
        {
            Email = "test@example.com",
            Password = "correct-password"
        });

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(authResult, ok.Value);
    }

    [Fact]
    public async Task GetUserUID_WhenTokenCannotBeResolved_Returns404()
    {
        _authService.Setup(x => x.GetUserUIDAsync("bad-token")).ReturnsAsync((string?)null);

        var result = await _sut.GetUserUID("bad-token");

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetUserUID_WhenTokenCanBeResolved_ReturnsUid()
    {
        _authService.Setup(x => x.GetUserUIDAsync("good-token")).ReturnsAsync("uid-1");

        var result = await _sut.GetUserUID("good-token");

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("uid-1", ok.Value!.GetType().GetProperty("uid")!.GetValue(ok.Value));
    }
}
