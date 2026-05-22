# Klima-Kontrolloeren

Smart home climate monitoring with Raspberry Pi sensors, an ASP.NET Core API, and a Vue dashboard.

> Code freeze: 12:00, Friday 22 May 2026.

[![Backend CI/CD](https://github.com/OneHandedWonder/Klima-Kontrolloeren/actions/workflows/main_klimakontrolloeren-backend.yml/badge.svg?branch=main)](https://github.com/OneHandedWonder/Klima-Kontrolloeren/actions/workflows/main_klimakontrolloeren-backend.yml)
[![Frontend CI/CD](https://github.com/OneHandedWonder/Klima-Kontrolloeren/actions/workflows/main_klimakontrolloeren-frontend.yml/badge.svg?branch=main)](https://github.com/OneHandedWonder/Klima-Kontrolloeren/actions/workflows/main_klimakontrolloeren-frontend.yml)
[![Test Backend CI/CD](https://github.com/OneHandedWonder/Klima-Kontrolloeren/actions/workflows/test_testklimakontrolloerenbackend.yml/badge.svg?branch=Test)](https://github.com/OneHandedWonder/Klima-Kontrolloeren/actions/workflows/test_testklimakontrolloerenbackend.yml)
[![Test Frontend CI/CD](https://github.com/OneHandedWonder/Klima-Kontrolloeren/actions/workflows/test_testklimakontrolloerenfrontend.yml/badge.svg?branch=Test)](https://github.com/OneHandedWonder/Klima-Kontrolloeren/actions/workflows/test_testklimakontrolloerenfrontend.yml)

## Overview

Klima-Kontrolloeren collects indoor climate readings from Raspberry Pi sensors and makes them available through a web dashboard. The system tracks temperature, humidity, and CO2 readings, supports authenticated user-specific sensor access, and exposes API endpoints for raw readings, aggregated climate data, sensor administration, and CSV exports.

Newest additions include profile-based sensor management, per-user and per-sensor CSV downloads, dashboard caching, custom comfort thresholds, weather location search with favorites, disabled-user handling, and API rate limiting.

## Features

- Raspberry Pi sensor ingestion for temperature, humidity, and CO2 readings.
- Authenticated Vue dashboard with Firebase sign-in and protected routes.
- User-specific sensor lists with sensor names, types, and locations.
- Profile page for changing email/password and adding, editing, removing, or exporting sensors.
- Dashboard cards for current indoor readings, multi-sensor status, historical averages, and weather.
- Custom comfort ranges for temperature and humidity with local persistence and warning confirmations for risky values.
- Open-Meteo weather integration with city search, local favorites, current weather, and forecast data.
- Cached dashboard data for user, sensor, average, and weather requests to reduce repeated API calls.
- CSV export endpoints for all readings, a user's readings, or one sensor's readings.
- Fixed-window API rate limiting for sign-in, token lookup, data reads, sensor reads, and sensor ingestion.
- xUnit, integration, Selenium, and CI coverage for backend, frontend, and deployment workflows.

## Stack

| Layer | Technology |
| --- | --- |
| Frontend | Vue 3, Vite, Vue Router, Firebase client |
| Backend | ASP.NET Core, .NET 9, SQL Server client |
| Sensor app | Python Raspberry Pi sensor sender |
| Tests | xUnit, Moq, Selenium, ASP.NET Core test host, coverage collector |
| CI/CD | GitHub Actions, Azure App Service |

## Repository Layout

```text
Klima-Kontrolloeren-Backend/    ASP.NET Core API
Klima-Kontrolloeren-Frontend/   Vue dashboard
Senson_app/                     Raspberry Pi sensor sender script
xUnit Test/                     Unit, integration, and Selenium tests
.github/workflows/              Main and Test CI/CD pipelines
```

## API Highlights

| Area | Endpoints |
| --- | --- |
| Auth | `POST /api/auth/signin`, `GET /api/auth/getUserUID` |
| Sensor ingestion | `POST /api/sensor` |
| Sensor management | `GET /api/sensor`, `GET /api/sensor/info`, `PUT /api/sensor/info`, `POST /api/sensor/add`, `DELETE /api/sensor/remove` |
| Data reads | `GET /api/data`, `GET /api/data/daylyAverage`, `GET /api/data/weeklyAverage`, `GET /api/data/monthlyAverage` |
| CSV export | `GET /api/data/export`, `GET /api/data/export/user`, `GET /api/data/export/sensor` |

The data endpoints accept a `uid` query parameter where relevant so dashboard views and exports can be scoped to the signed-in user's sensors. Sensor CSV export also validates that the requested sensor belongs to the user before returning data.

## Environments

| Environment | Branch | Backend deploy | Frontend deploy |
| --- | --- | --- | --- |
| Production | `main` | `KlimaKontrolloeren-Backend` | `KlimaKontrolloeren-Frontend` |
| Test | `Test` | `TestKlimaKontrolloerenBackend` | `TestKlimaKontrolloerenFrontend` |

Pull requests run build and test checks. Pushes to `main` and `Test` deploy to their matching Azure App Service environments after the checks pass.

## Build and Test Status

| Environment | Backend workflow | Frontend workflow |
| --- | --- | --- |
| Production | [`main_klimakontrolloeren-backend.yml`](https://github.com/OneHandedWonder/Klima-Kontrolloeren/actions/workflows/main_klimakontrolloeren-backend.yml) | [`main_klimakontrolloeren-frontend.yml`](https://github.com/OneHandedWonder/Klima-Kontrolloeren/actions/workflows/main_klimakontrolloeren-frontend.yml) |
| Test | [`test_testklimakontrolloerenbackend.yml`](https://github.com/OneHandedWonder/Klima-Kontrolloeren/actions/workflows/test_testklimakontrolloerenbackend.yml) | [`test_testklimakontrolloerenfrontend.yml`](https://github.com/OneHandedWonder/Klima-Kontrolloeren/actions/workflows/test_testklimakontrolloerenfrontend.yml) |

The backend workflows publish a GitHub Actions run summary with total, passed, failed, and skipped tests. Test result files and coverage reports are uploaded as workflow artifacts.

## Local Commands

```bash
# Backend build and tests
dotnet restore Klima-Kontrolloeren-Backend.sln
dotnet build Klima-Kontrolloeren-Backend.sln --configuration Release
dotnet test Klima-Kontrolloeren-Backend.sln --configuration Release

# Frontend install and build
cd Klima-Kontrolloeren-Frontend
npm ci
npm run build
```

## CI Notes

- Documentation-only changes, including `README.md`, are ignored by workflow triggers.
- Backend test runs include the Selenium frontend tests.
- The Test frontend deployment packages the Vue build with a small Node static server for Azure Linux App Service.
