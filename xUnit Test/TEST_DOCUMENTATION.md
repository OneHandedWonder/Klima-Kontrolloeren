# Test Documentation

This test project contains backend tests written with xUnit and frontend browser tests written with Selenium.

Run all tests from the `Klima-Kontrolloeren` folder:

```bash
dotnet test "xUnit Test/TestProject.csproj"
```

Run all tests with coverage:

```bash
dotnet test "xUnit Test/TestProject.csproj" --collect:"XPlat Code Coverage" --results-directory "xUnit Test/TestResults"
```

Generate an HTML coverage report:

```bash
reportgenerator -reports:"xUnit Test/TestResults/*/coverage.cobertura.xml" -targetdir:"CoverageReport" -reporttypes:Html
```

## Backend Tests

### AuthControllerTests

File: `Controllers/AuthControllerTests.cs`

Tests the authentication API controller.

What is tested:

- Sign in returns `400 Bad Request` when email or password is missing.
- Sign in returns `401 Unauthorized` when the auth service rejects the credentials.
- Sign in returns `200 OK` with an auth result when credentials are valid.
- Getting a user UID returns `404 Not Found` when the token cannot be resolved.
- Getting a user UID returns `200 OK` when the token resolves successfully.

### DataControllerTests

File: `Controllers/DataControllerTests.cs`

Tests the sensor data API controller.

What is tested:

- Reading data with an invalid limit returns `400 Bad Request`.
- Reading data without a UID calls the general readings service method.
- Reading data with a UID calls the user-filtered readings service method.
- Daily readings return 24 hourly average buckets.
- Daily readings with a UID call the user-filtered service method.
- Weekly readings return seven daily average buckets.
- Weekly readings with a UID call the user-filtered service method.
- Monthly readings return 30 daily average buckets.
- Monthly readings with a UID call the user-filtered service method.
- Hourly averages are calculated correctly for temperature, humidity, and CO2.

### SensorControllerTests

File: `Controllers/SensorControllerTests.cs`

Tests the sensor API controller.

What is tested:

- Posting a valid sensor reading returns `201 Created`.
- Posting a reading without a `SensorId` returns `400 Bad Request`.
- Getting user sensors without a UID returns `400 Bad Request`.
- Getting user sensors when none exist returns `404 Not Found`.
- Stored sensor strings are parsed correctly from JSON-array format.
- Stored sensor strings are parsed correctly from comma-separated format.
- Stored blank sensor strings return an empty sensor array.

### SensorServiceTests

File: `Services/SensorServiceTests.cs`

Tests the sensor service layer.

What is tested:

- Saving a valid sensor reading calls the database layer once.
- Getting readings with a valid limit returns the database result.
- Getting readings with a UID calls the user-filtered database method.
- Getting readings when no data exists returns an empty list.
- Getting readings with an invalid limit throws an `ArgumentException`.
- Daily, weekly, and monthly reading methods return database results.
- Daily, weekly, and monthly UID overloads reject empty UIDs.
- Getting user sensors returns the database result.

### AuthServiceTests

File: `Services/AuthServiceTests.cs`

Tests the authentication service layer without calling Firebase directly.

What is tested:

- Sign in returns `null` when email or password is missing.
- Sign in throws when `Firebase:ApiKey` is not configured.
- Sign in returns `null` when Firebase rejects the credentials.
- Sign in returns `null` when the database user is missing or disabled.
- Sign in returns an auth result for an enabled user.
- Non-numeric Firebase expiry values are handled as `0`.
- Getting user sensors returns `null` for an empty token.
- Getting user sensors returns users from the database.
- Getting a UID returns `null` for empty tokens, missing API keys, failed Firebase responses, empty Firebase users, or HTTP failures.
- Getting a UID returns the Firebase `localId` when lookup succeeds.

### AverageServiceTests

File: `Services/AverageServiceTests.cs`

Tests average calculation logic.

What is tested:

- Daily averages return seven days of average data.
- Hourly averages return 24 hours of average data.
- Temperature, humidity, and CO2 averages are calculated correctly.
- Weekly average output returns 30 days.
- Missing days return `null` average values.

### DbConnectionFactoryTests

File: `Data/DbConnectionFactoryTests.cs`

Tests safe configuration behavior for the SQL connection factory.

What is tested:

- Missing `DefaultConnection` throws an `InvalidOperationException`.
- A configured `DefaultConnection` creates the factory.
- Sensor string parsing supports JSON arrays, comma-separated values, single values, and blank values.

The actual SQL query methods are excluded from unit-test coverage because they require SQL Server behavior. Those should be covered with integration tests against a test SQL database or SQL Server container.

## Backend Integration Tests

### BackendStartupTests

File: `Integration/BackendStartupTests.cs`

Tests that the ASP.NET backend can start through `WebApplicationFactory`.

What is tested:

- The root endpoint `/` returns the expected backend running message.

## Model Tests

### AuthModelsTests

File: `Models/AuthModelsTests.cs`

Tests authentication-related DTO contracts.

What is tested:

- `SignInRequest` default values are empty strings.
- `SignInRequest` deserializes `email` and `password` JSON properties.
- `AuthResult` serializes the expected JSON property names.
- `KlimaDataUser` deserializes `uid`, `sensors`, and `enabled` JSON properties.

### SensorModelsTests

File: `Models/SensorModelsTests.cs`

Tests sensor and average DTO behavior.

What is tested:

- `SensorReading` default values are correct.
- `SensorReading` stores assigned values.
- `SensorReadingDto` stores assigned values.
- `AverageData` stores assigned values, including nullable averages.

## Frontend Selenium Tests

### SeleniumSignInTests

File: `Frontend/SeleniumSignInTests.cs`

Tests the Vue frontend in a real browser using Selenium and Chrome.

The test fixture starts the Vite frontend automatically on an available local port before the browser tests run.

What is tested:

- The sign-in page loads with the expected title, email input, password input, and submit button.
- The user can switch from sign-in mode to create-account mode.
- The user can open forgot-password mode.
- The user can return from forgot-password mode back to sign-in mode.

## Test Dependencies

The test project uses:

- xUnit for test execution.
- Moq for mocking backend services and database dependencies.
- Selenium WebDriver for frontend browser automation.
- ChromeDriver through Selenium Manager.

The Selenium tests require Google Chrome or Chromium to be installed locally.
