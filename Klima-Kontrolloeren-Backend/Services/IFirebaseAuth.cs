namespace KlimaKontrolloerenBackend.Services;

public interface IFirebaseAuth
{
    Task<string> VerifyIdTokenAsync(string idToken);
}