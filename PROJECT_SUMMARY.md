# Project Summary

## Overview

Klima-Kontrolloeren is a smart home climate monitoring system. The project is built to collect indoor climate measurements from Raspberry Pi sensors and make the data available through a web dashboard.

The system focuses on three types of climate data:

- Temperature
- Humidity
- CO2

The goal of the project is to give users an overview of their indoor climate and make it possible to monitor changes over time.

## Main Parts of the System

The project consists of three main parts:

| Part | Description |
| --- | --- |
| Sensor app | Python application that runs on a Raspberry Pi and sends sensor readings |
| Backend API | ASP.NET Core API that receives, processes, and returns climate data |
| Frontend dashboard | Vue web application where users can sign in and view climate data |

## Sensor App

The sensor app is responsible for collecting climate data from the physical sensors. It sends readings to the backend API, including the sensor ID and measured values.

This part represents the data source of the system. Without the sensor app, the backend and frontend would not have live climate data to work with.

## Backend

The backend is built with ASP.NET Core and .NET 9. It works as the central part of the system.

The backend is responsible for:

- Receiving sensor readings from the Raspberry Pi.
- Validating incoming sensor data.
- Reading and writing climate data through the database layer.
- Handling user authentication.
- Returning user-specific sensor data.
- Calculating daily, weekly, and monthly averages.
- Exposing API endpoints for the frontend.

The backend is structured with controllers, services, models, and a database connection layer. This separation makes the code easier to test and maintain.

## Frontend

The frontend is built with Vue 3 and Vite. It provides the user interface for the system.

The frontend is responsible for:

- Showing the sign-in page.
- Supporting create-account and forgot-password views.
- Connecting to Firebase on the client side.
- Displaying climate data in a dashboard.
- Communicating with the backend API.

The frontend gives users a visual way to interact with the climate monitoring system instead of only using raw API endpoints.

## Authentication

The project uses Firebase-related authentication. Users sign in through the frontend, and the backend verifies authentication-related information when needed.

The backend also checks whether a user is enabled and which sensors belong to that user. This means users should only see data from sensors connected to their own account.

## Data Flow

The general data flow is:

1. A Raspberry Pi sensor collects temperature, humidity, and CO2 values.
2. The Python sensor app sends the reading to the backend API.
3. The backend validates and stores the reading.
4. A user signs in through the frontend.
5. The frontend requests climate data from the backend.
6. The backend returns raw readings or calculated averages.
7. The frontend displays the data in the dashboard.

## Technologies Used

| Area | Technology |
| --- | --- |
| Backend | ASP.NET Core, .NET 9 |
| Frontend | Vue 3, Vite, Vue Router |
| Sensor app | Python |
| Authentication | Firebase |
| Database access | SQL Server client |
| Testing | xUnit, Moq, Selenium |
| Deployment/CI | GitHub Actions, Azure App Service |

## Project Structure

```text
Klima-Kontrolloeren-Backend/     Backend API
Klima-Kontrolloeren-Frontend/    Vue frontend
Senson_app/                      Python sensor application
xUnit Test/                      Automated tests
```

## Current Strengths

The project has a clear separation between frontend, backend, sensor app, and tests. The backend uses interfaces and services, which makes it easier to test important logic without depending directly on external systems.

The system also includes automated tests for backend controllers, services, models, startup behavior, and selected frontend flows. This gives the project a stronger foundation and makes future changes safer.

## Current Limitations

Some areas could be expanded further:

- The Python sensor app has less automated test coverage than the backend.
- The database layer would benefit from integration tests using a real test database.
- The frontend tests currently focus mainly on the sign-in page.
- Full end-to-end tests with real sensor data and authenticated dashboard usage could improve confidence.

## Summary

Klima-Kontrolloeren is a full-stack climate monitoring project that connects physical sensors, a backend API, authentication, and a web dashboard. The system collects sensor readings, connects them to users, calculates averages, and presents the climate data through a frontend interface.

Overall, the project demonstrates how a real-world monitoring system can be built by combining hardware data collection, backend processing, authentication, frontend visualization, and automated testing.
