# Klima-Kontrolloeren

Smart home climate monitoring with Raspberry Pi sensors, an ASP.NET Core API, and a Vue dashboard.

[![Backend CI/CD](https://github.com/OneHandedWonder/Klima-Kontrolloeren/actions/workflows/main_klimakontrolloeren-backend.yml/badge.svg?branch=main)](https://github.com/OneHandedWonder/Klima-Kontrolloeren/actions/workflows/main_klimakontrolloeren-backend.yml)
[![Frontend CI/CD](https://github.com/OneHandedWonder/Klima-Kontrolloeren/actions/workflows/main_klimakontrolloeren-frontend.yml/badge.svg?branch=main)](https://github.com/OneHandedWonder/Klima-Kontrolloeren/actions/workflows/main_klimakontrolloeren-frontend.yml)
[![Test Backend CI/CD](https://github.com/OneHandedWonder/Klima-Kontrolloeren/actions/workflows/test_testklimakontrolloerenbackend.yml/badge.svg?branch=Test)](https://github.com/OneHandedWonder/Klima-Kontrolloeren/actions/workflows/test_testklimakontrolloerenbackend.yml)
[![Test Frontend CI/CD](https://github.com/OneHandedWonder/Klima-Kontrolloeren/actions/workflows/test_testklimakontrolloerenfrontend.yml/badge.svg?branch=Test)](https://github.com/OneHandedWonder/Klima-Kontrolloeren/actions/workflows/test_testklimakontrolloerenfrontend.yml)

## Overview

Klima-Kontrolloeren collects indoor climate readings from Raspberry Pi sensors and makes them available through a web dashboard. The system tracks temperature, humidity, and CO2 readings, supports user-specific sensor access, and exposes API endpoints for raw readings and aggregated climate data.

## Stack

| Layer | Technology |
| --- | --- |
| Frontend | Vue 3, Vite, Vue Router, Firebase client |
| Backend | ASP.NET Core, .NET 9, SQL Server client |
| Tests | xUnit, Moq, Selenium, ASP.NET Core test host, coverage collector |
| CI/CD | GitHub Actions, Azure App Service |

## Repository Layout

```text
Klima-Kontrolloeren-Backend/    ASP.NET Core API
Klima-Kontrolloeren-Frontend/   Vue dashboard
xUnit Test/                     Unit, integration, and Selenium tests
.github/workflows/              Main and Test CI/CD pipelines
```

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
