using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Globalization;
using System.Text.Json;

namespace Klima_Kontrolloeren_Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExternalWeatherController : ControllerBase
{
	private const string VisualCrossingKeyName = "WEATHER_API_KEY";

	[HttpGet("report")]
	public async Task<ActionResult<WeatherReport>> GetReport([FromQuery] string city, [FromQuery] int days = 7, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(city))
		{
			return BadRequest("City is required.");
		}

		if (days < 1 || days > 7)
		{
			return BadRequest("Days must be between 1 and 7.");
		}

		var visualCrossingKey = Environment.GetEnvironmentVariable(VisualCrossingKeyName);
		if (string.IsNullOrWhiteSpace(visualCrossingKey))
		{
			return StatusCode(StatusCodes.Status500InternalServerError, $"Missing environment variable: {VisualCrossingKeyName}");
		}

		var encodedCity = WebUtility.UrlEncode(city);
		var requestUrl = $"https://weather.visualcrossing.com/VisualCrossingWebServices/rest/services/timeline/{encodedCity}?unitGroup=metric&include=current%2Calerts%2Cdays%2Cevents%2Chours&key={visualCrossingKey}&contentType=json";

		using var httpClient = new HttpClient();
		using var response = await httpClient.GetAsync(requestUrl, cancellationToken);
		var payload = await response.Content.ReadAsStringAsync(cancellationToken);

		if (!response.IsSuccessStatusCode)
		{
			return StatusCode((int)response.StatusCode, payload);
		}

		using var document = JsonDocument.Parse(payload);
		var root = document.RootElement;
		var currentConditions = root.GetProperty("currentConditions");
		var forecastDays = root.GetProperty("days");
		var requestedDays = forecastDays.EnumerateArray().Take(days).ToArray();

		var report = new WeatherReport
		{
			City = city,
			TemperatureCurrent = currentConditions.GetProperty("temp").GetDouble(),
			Condition = currentConditions.GetProperty("conditions").GetString() ?? string.Empty,
			warnings = BuildWarnings(root),
			humidity = currentConditions.GetProperty("humidity").GetDouble().ToString(CultureInfo.InvariantCulture),
			Temperatures = BuildHourlyTemperatures(requestedDays),
			HourlyRainChance = BuildHourlyRainChance(requestedDays),
			Stations = BuildStations(root)
		};

		return Ok(report);
	}

	private static string BuildWarnings(JsonElement root)
	{
		if (!root.TryGetProperty("alerts", out var alerts) || alerts.ValueKind != JsonValueKind.Array)
		{
			return string.Empty;
		}

		var warningTexts = alerts.EnumerateArray()
			.Select(alert =>
			{
				if (alert.TryGetProperty("event", out var eventProperty))
				{
					return eventProperty.GetString() ?? string.Empty;
				}

				if (alert.TryGetProperty("headline", out var headlineProperty))
				{
					return headlineProperty.GetString() ?? string.Empty;
				}

				return string.Empty;
			})
			.Where(text => !string.IsNullOrWhiteSpace(text));

		return string.Join("; ", warningTexts);
	}

	private static Dictionary<string, float> BuildHourlyTemperatures(IEnumerable<JsonElement> days)
	{
		return days
			.SelectMany(day =>
			{
				if (!day.TryGetProperty("datetime", out var dayDate) || !day.TryGetProperty("hours", out var hours) || hours.ValueKind != JsonValueKind.Array)
				{
					return Enumerable.Empty<KeyValuePair<string, float>>();
				}

				var datePrefix = dayDate.GetString() ?? string.Empty;
				return hours.EnumerateArray()
					.Where(hour => hour.TryGetProperty("datetime", out _) && hour.TryGetProperty("temp", out _))
					.Select(hour => new KeyValuePair<string, float>(
						$"{datePrefix} {hour.GetProperty("datetime").GetString()}",
						(float)hour.GetProperty("temp").GetDouble()));
			})
			.ToDictionary(pair => pair.Key, pair => pair.Value);
	}

	private static Dictionary<string, float> BuildHourlyRainChance(IEnumerable<JsonElement> days)
	{
		return days
			.SelectMany(day =>
			{
				if (!day.TryGetProperty("datetime", out var dayDate) || !day.TryGetProperty("hours", out var hours) || hours.ValueKind != JsonValueKind.Array)
				{
					return Enumerable.Empty<KeyValuePair<string, float>>();
				}

				var datePrefix = dayDate.GetString() ?? string.Empty;
				return hours.EnumerateArray()
					.Where(hour => hour.TryGetProperty("datetime", out _) && hour.TryGetProperty("precipprob", out _))
					.Select(hour => new KeyValuePair<string, float>(
						$"{datePrefix} {hour.GetProperty("datetime").GetString()}",
						(float)hour.GetProperty("precipprob").GetDouble()));
			})
			.ToDictionary(pair => pair.Key, pair => pair.Value);
	}

	private static Dictionary<string, WeatherStation> BuildStations(JsonElement root)
	{
		if (!root.TryGetProperty("stations", out var stations) || stations.ValueKind != JsonValueKind.Object)
		{
			return new Dictionary<string, WeatherStation>();
		}

		return stations.EnumerateObject()
			.Select(station => new KeyValuePair<string, WeatherStation>(
				station.Name,
				new WeatherStation
				{
					Distance = station.Value.TryGetProperty("distance", out var distanceProperty) ? distanceProperty.GetDouble() : 0,
					Latitude = station.Value.TryGetProperty("latitude", out var latitudeProperty) ? latitudeProperty.GetDouble() : 0,
					Longitude = station.Value.TryGetProperty("longitude", out var longitudeProperty) ? longitudeProperty.GetDouble() : 0,
					UseCount = station.Value.TryGetProperty("useCount", out var useCountProperty) ? useCountProperty.GetInt32() : 0,
					Id = station.Value.TryGetProperty("id", out var idProperty) ? idProperty.GetString() ?? string.Empty : string.Empty,
					Name = station.Value.TryGetProperty("name", out var nameProperty) ? nameProperty.GetString() ?? string.Empty : string.Empty,
					Quality = station.Value.TryGetProperty("quality", out var qualityProperty) ? qualityProperty.GetInt32() : 0,
					Contribution = station.Value.TryGetProperty("contribution", out var contributionProperty) ? contributionProperty.GetDouble() : 0
				}))
			.ToDictionary(pair => pair.Key, pair => pair.Value);
	}
}