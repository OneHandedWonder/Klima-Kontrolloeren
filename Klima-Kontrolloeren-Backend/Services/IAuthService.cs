using KlimaKontrolloerenBackend.Models;

namespace KlimaKontrolloerenBackend.Services;

public interface IAuthService
{
    Task<AuthResult?> SignInAsync(string email, string password, CancellationToken cancellationToken = default);
    Task<List<KlimaDataUser>?> GetUserSensorsAsync(string uid);
    Task<string?> GetUserUIDAsync(string token);
}