using Moq;
using KlimaKontrolloerenBackend.Services;
using Xunit;

namespace KlimaKontrolloerenBackend.Tests.Services;

public class FirebaseServiceTests
{
    private readonly Mock<IFirebaseAuth> _mockFirebaseAuth;
    private readonly FirebaseService _sut;

    public FirebaseServiceTests()
    {
        _mockFirebaseAuth = new Mock<IFirebaseAuth>();
        _sut = new FirebaseService(_mockFirebaseAuth.Object);
    }

    [Fact]
    public async Task VerifyTokenAsync_ValidToken_ReturnsUid()
    {
        // Arrange
        const string token = "valid-firebase-token";
        const string expectedUid = "firebase-uid-123";

        _mockFirebaseAuth
            .Setup(x => x.VerifyIdTokenAsync(token))
            .ReturnsAsync(expectedUid);

        // Act
        var result = await _sut.VerifyTokenAsync(token);

        // Assert
        Assert.Equal(expectedUid, result);
    }

    [Fact]
    public async Task VerifyTokenAsync_InvalidToken_ReturnsNull()
    {
        // Arrange
        _mockFirebaseAuth
            .Setup(x => x.VerifyIdTokenAsync("invalid-token"))
            .ThrowsAsync(new Exception("Firebase token verification failed."));

        // Act
        var result = await _sut.VerifyTokenAsync("invalid-token");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task VerifyTokenAsync_EmptyToken_ReturnsNull()
    {
        // Arrange
        _mockFirebaseAuth
            .Setup(x => x.VerifyIdTokenAsync(string.Empty))
            .ThrowsAsync(new Exception("Firebase token verification failed."));

        // Act
        var result = await _sut.VerifyTokenAsync(string.Empty);

        // Assert
        Assert.Null(result);
    }
}