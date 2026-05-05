namespace KlimaKontrolloerenBackend.Services;

public interface IFirebaseService
{
    Task<string?> VerifyTokenAsync(string idToken);
}