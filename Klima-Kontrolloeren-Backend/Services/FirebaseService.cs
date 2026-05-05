using FirebaseAdmin.Auth;

namespace KlimaKontrolloerenBackend.Services;

public class FirebaseAuthWrapper : IFirebaseAuth
{
    public async Task<string> VerifyIdTokenAsync(string idToken)
    {
        var token = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(idToken);
        return token.Uid;
    }
}

public class FirebaseService : IFirebaseService
{
    private readonly IFirebaseAuth _firebaseAuth;

    public FirebaseService(IFirebaseAuth firebaseAuth)
    {
        _firebaseAuth = firebaseAuth;
    }

    public async Task<string?> VerifyTokenAsync(string idToken)
    {
        try
        {
            return await _firebaseAuth.VerifyIdTokenAsync(idToken);
        }
        catch
        {
            return null;
        }
    }
}