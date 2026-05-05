using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using KlimaKontrolloerenBackend.Data;
using KlimaKontrolloerenBackend.Services;

var builder = WebApplication.CreateBuilder(args);

FirebaseApp.Create(new AppOptions
{
    Credential = GoogleCredential.FromFile("firebase-service-account.json")
});

builder.Services.AddControllers();
builder.Services.AddScoped<IDbConnectionFactory, DbConnectionFactory>();
builder.Services.AddScoped<IFirebaseAuth, FirebaseAuthWrapper>();
builder.Services.AddScoped<ISensorService, SensorService>();       // ? updated
builder.Services.AddScoped<IFirebaseService, FirebaseService>();   // ? updated

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod()));

var app = builder.Build();

app.UseCors();
app.MapControllers();
app.Run();