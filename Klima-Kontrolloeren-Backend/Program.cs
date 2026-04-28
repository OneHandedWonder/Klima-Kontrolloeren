using Microsoft.OpenApi.Models;

LoadDotEnvFile();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "DR-Repo API", Version = "v1" });
});

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();

static void LoadDotEnvFile()
{
    var searchPaths = new[]
    {
        Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), ".env")),
        Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", ".env")),
    };

    var envFilePath = searchPaths.FirstOrDefault(File.Exists);
    if (envFilePath is null)
    {
        return;
    }

    foreach (var line in File.ReadAllLines(envFilePath))
    {
        var trimmedLine = line.Trim();
        if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith('#'))
        {
            continue;
        }

        var equalsIndex = trimmedLine.IndexOf('=');
        if (equalsIndex <= 0)
        {
            continue;
        }

        var key = trimmedLine[..equalsIndex].Trim();
        var value = trimmedLine[(equalsIndex + 1)..].Trim().Trim('"');

        if (!string.IsNullOrWhiteSpace(key) && Environment.GetEnvironmentVariable(key) is null)
        {
            Environment.SetEnvironmentVariable(key, value);
        }
    }
}