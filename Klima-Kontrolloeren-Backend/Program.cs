using KlimaKontrolloerenBackend.Data;
using KlimaKontrolloerenBackend.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();
builder.Services.AddScoped<IDbConnectionFactory, DbConnectionFactory>();
builder.Services.AddScoped<ISensorService, SensorService>();
builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod()));

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();


app.UseCors();
app.MapGet("/", () => Results.Ok("Klima-Kontrolloeren backend is running."));
app.MapControllers();
app.Run();

public partial class Program { }
