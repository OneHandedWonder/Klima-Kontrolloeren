# Project and Test Summary

## Project Overview

Klima-Kontrolloeren is a smart home climate monitoring system. The project collects indoor climate data from Raspberry Pi sensors and shows the data in a web dashboard. The measured values are temperature, humidity, and CO2.

The system is split into three main parts:

- **Backend:** An ASP.NET Core API built with .NET 9.
- **Frontend:** A Vue 3 application built with Vite.
- **Sensor app:** A Python application that sends sensor readings to the backend.

The backend receives sensor readings, stores and reads climate data, handles authentication, and returns both raw readings and calculated averages. The frontend lets users sign in and view the climate dashboard.

## Testing Strategy

The project uses several types of tests instead of relying on only one testing method. This gives better coverage because different parts of the system are tested at different levels.

The test project is located in:

```text
xUnit Test/
```

The main test framework is **xUnit**. The backend tests are written in C#, and the frontend browser tests are automated with **Selenium WebDriver**.

The project uses these main testing tools:

| Tool | Purpose |
| --- | --- |
| xUnit | Runs unit tests, integration tests, and test assertions |
| Moq | Creates fake versions of services and database dependencies |
| Selenium WebDriver | Tests the frontend in a real browser |
| ChromeDriver / Selenium Manager | Runs the Selenium tests in Chrome |
| WebApplicationFactory | Starts the ASP.NET Core backend in a test host |
| coverlet.collector | Collects code coverage from the test run |

## Unit Testing

Most of the tests are unit tests. A unit test checks a small part of the system in isolation, for example one service method, one controller action, or one model.

The purpose of the unit tests is to confirm that each class behaves correctly without needing the full application, a real database, or a real browser.

### Controller Tests

The controller tests check the API behavior from the outside of each controller method. They verify that the backend returns the correct HTTP result depending on the input and service response.

The tested controllers are:

- `AuthController`
- `DataController`
- `SensorController`

Examples of what is tested:

- Missing email or password returns `400 Bad Request`.
- Invalid login returns `401 Unauthorized`.
- Valid login returns `200 OK`.
- Posting a valid sensor reading returns `201 Created`.
- Posting a sensor reading without a sensor ID returns `400 Bad Request`.
- Requesting readings with an invalid limit returns `400 Bad Request`.
- User-specific data requests call the user-specific service methods.
- Daily, weekly, and monthly average endpoints return the expected number of average buckets.

These tests use **Moq** to replace the real services with mocked services. This means the controller tests focus only on controller logic and HTTP responses.

### Service Tests

The service tests check the business logic between the API controllers and the database layer.

The tested services are:

- `SensorService`
- `AuthService`
- `AverageService`

Examples of what is tested:

- A valid sensor reading is passed to the database layer.
- Invalid limits throw an `ArgumentException`.
- Empty user IDs are rejected for user-specific methods.
- Authentication returns `null` when credentials are invalid.
- Authentication rejects disabled or missing users.
- Firebase response handling works for both successful and failed responses.
- Average calculations return correct temperature, humidity, and CO2 values.
- Missing time periods return `null` average values.

The service tests also use **Moq** to mock database and HTTP dependencies. This makes the tests faster and more predictable because they do not depend on external systems such as SQL Server or Firebase.

### Model Tests

The model tests check the data contracts used by the backend. These tests are important because the frontend, backend, database, and Firebase responses depend on consistent property names and default values.

Examples of what is tested:

- `SignInRequest` has the expected default values.
- JSON deserialization maps `email` and `password` correctly.
- `AuthResult` serializes with the expected JSON property names.
- `SensorReading` and `SensorReadingDto` store assigned values correctly.
- `AverageData` supports nullable average values.

These tests help catch errors where a property name or DTO structure changes and breaks communication between system layers.

## Mocking

Mocking is used heavily in the backend unit tests. The project uses **Moq** to create fake versions of dependencies such as:

- `ISensorService`
- `IAuthService`
- `IDbConnectionFactory`
- `IHttpClientFactory`

Mocking is used so the tests can control what dependencies return. For example, an authentication test can simulate a failed Firebase login without actually calling Firebase.

This method makes the tests:

- Faster, because no real network or database call is needed.
- More stable, because they do not depend on external services being available.
- More focused, because each test checks only the class under test.

## Integration Testing

The project also includes an integration test for backend startup.

The integration test uses **WebApplicationFactory** from ASP.NET Core. This starts the backend in a test environment and sends an HTTP request to it.

The test checks that:

- The backend application can start correctly.
- The root endpoint `/` responds successfully.
- The response contains the expected backend running message.

This test is broader than a unit test because it checks that routing, dependency registration, middleware, and the application startup pipeline work together.

## Frontend Browser Testing

The frontend is tested with **Selenium WebDriver**. These tests run the Vue application in a real Chrome browser in headless mode.

The Selenium test fixture starts the Vite development server automatically on an available local port. The tests then open the sign-in page and interact with the page like a user would.

Examples of what is tested:

- The sign-in page loads with the expected title.
- The email input is visible.
- The password input is visible.
- The submit button is visible.
- The user can switch from sign-in mode to create-account mode.
- The user can open forgot-password mode.
- The user can return from forgot-password mode back to sign-in mode.

This type of test is useful because it checks the actual rendered frontend, not just isolated JavaScript logic. It confirms that important user interface flows work in a browser.

## Test Coverage

The test project supports code coverage through `coverlet.collector`.

Tests can be run with coverage using:

```bash
dotnet test "xUnit Test/TestProject.csproj" --collect:"XPlat Code Coverage" --results-directory "xUnit Test/TestResults"
```

An HTML coverage report can then be generated with:

```bash
reportgenerator -reports:"xUnit Test/TestResults/*/coverage.cobertura.xml" -targetdir:"CoverageReport" -reporttypes:Html
```

Coverage helps show which parts of the backend are tested and which parts may need more test cases.

## Test Boundaries and Limitations

The test suite covers controllers, services, models, backend startup, and selected frontend flows. However, not every part of the system is tested in the same way.

The SQL query methods are not fully unit tested because they depend on real SQL Server behavior. These would be better tested with integration tests using a real test database or a SQL Server container.

The Selenium tests cover important sign-in page behavior, but they do not test the complete dashboard flow with real authenticated users and live sensor data.

The Python sensor app is part of the project, but the current test suite mainly focuses on the .NET backend and Vue frontend.

## How to Run the Tests

From the `Klima-Kontrolloeren` folder, all tests can be run with:

```bash
dotnet test "xUnit Test/TestProject.csproj"
```

The Selenium tests require Google Chrome or Chromium to be installed locally.

## Summary

The project uses a layered testing approach. Unit tests check the backend logic in isolation, mocked dependencies make the tests fast and stable, integration tests confirm that the backend starts correctly, and Selenium tests verify important frontend behavior in a real browser.

This gives the project a stronger test foundation because it checks both internal logic and user-facing behavior. The strongest coverage is around the backend controllers, services, models, and authentication/data handling. The main areas for future improvement are deeper database integration tests, broader end-to-end dashboard tests, and tests for the Python sensor app.
