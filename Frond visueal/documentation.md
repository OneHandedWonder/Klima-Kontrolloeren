# Klima-Kontrolloeren — Project Documentation

**Zealand · 3. Semester Systemudvikling**

---

## Summary

Klima-Kontrolloeren is a web-based indoor climate monitoring dashboard.
It reads live sensor data (temperature, humidity, CO₂) from a Raspberry Pi
connected to an SCD30 sensor, and displays it in a browser alongside real
outdoor weather data fetched from the Open-Meteo API.

The dashboard gives users a clear, at-a-glance view of whether the indoor
climate is healthy, using a colour-coded system (green / orange / red) for
each measurement. It also shows a 5-day weather forecast and historical
trend graphs for all three sensor metrics.

---

## Project Structure

```
Frond visueal/
├── Klima-Kontrolloeren frond.html   — Main page, all UI structure
├── index.js                         — Vue 3 app logic, data fetching
├── stylesheet.css                   — All styling and animations
├── sensor_api.py                    — Flask API running on the Raspberry Pi
├── deploy_pi.py                     — Script to deploy sensor_api.py to the Pi
└── documentation.md                 — This file
```

---

## Technology Stack

| Layer       | Technology                          |
|-------------|-------------------------------------|
| Frontend    | HTML5, Vue 3 (CDN), Axios (CDN)     |
| Styling     | CSS3 (custom, no framework)         |
| Indoor data | Python Flask API on Raspberry Pi    |
| Outdoor data| Open-Meteo API (free, no key)       |
| Sensor      | SCD30 (CO₂, temperature, humidity)  |
| Protocol    | I2C (sensor → Pi)                   |
| Deployment  | SSH/SFTP via Paramiko, systemd      |

---

## File Explanations

---

### 1. Klima-Kontrolloeren frond.html

The single HTML page that makes up the entire dashboard.
It is structured as a Vue 3 application mounted on `<div id="app">`.

**Sections from top to bottom:**

#### Header
```html
<header class="header">
```
Displays the animated gradient title "Klima-Kontrolloeren" split into
three coloured spans (cyan → white → green) with a glowing animation
and a decorative line underneath.

#### Indoor Sensor Section
```html
<div class="indoor-section">
```
Contains three metric tiles: Temperature, Humidity, and CO₂.

Each tile has:
- A label and an **info icon (ⓘ)** — hovering shows a tooltip explaining
  the metric and its green / orange / red ranges
- A **large value number** that changes colour based on the reading
- A **colour-coded scale bar** with a white dot indicator showing where
  the current reading sits
- A **status badge** (Good / Warning / Poor)
- A **sensor status indicator** (green dot = online, red = offline,
  grey pulsing = connecting)

#### Outdoor Weather Card
```html
<div class="dashboard-card weather-card">
```
Shows live outdoor weather for Roskilde, DK from Open-Meteo:
- Large weather emoji + current temperature
- Weather description (e.g. "Light rain")
- Humidity % and wind speed (km/h)
- A divider line
- 5-day forecast tiles with emoji, day name, and max temperature

#### Graphs Card
```html
<div class="dashboard-card">
```
Bar chart showing historical sensor data.

Two rows of buttons control what is displayed:
- **Metric buttons** — Temperature / Humidity / CO₂
- **Period buttons** — Day / Week / Month

The chart is an SVG drawn dynamically by Vue with:
- A y-axis with grid lines and value labels (auto-scaled per metric)
- A unit label (°C / % / ppm)
- Bars with x-axis time labels

> Note: the graph currently shows placeholder data. Real historical data
> requires a logging system on the Pi (see Known Limitations).

#### Refresh Button
```html
<button @click="fetchAll">Refresh All</button>
```
Triggers both `fetchData()` (indoor sensor) and `fetchWeather()`
(outdoor weather) at once. Shows "Sensor updated: HH:MM:SS" with the
last time the Pi responded successfully.

#### Footer
Project name and course information.

---

### 2. index.js

The Vue 3 application logic. All data, computed properties, and methods
live here.

---

#### Constants (top of file)

```js
const OPEN_METEO_URL = '...'
```
The full Open-Meteo API URL for Roskilde, DK (lat 55.6415, lon 12.0803).
Requests current temperature, humidity, weather code, and wind speed,
plus a 6-day daily forecast.

```js
const WMO_DESCRIPTIONS = { ... }
```
Maps WMO weather codes (the international standard codes Open-Meteo
returns) to plain English descriptions like "Light rain" or "Overcast".

```js
const WMO_EMOJIS = { ... }
```
Maps the same WMO codes to display emojis (☀️ ⛅ 🌧️ ❄️ ⛈️ etc.)
used in both the current conditions display and the forecast tiles.

---

#### data()

All reactive variables the app uses:

| Variable         | Type    | Purpose                                      |
|------------------|---------|----------------------------------------------|
| `temperature`    | Number  | Indoor temp from Pi sensor (°C)              |
| `humidity`       | Number  | Indoor humidity from Pi sensor (%)           |
| `co2`            | Number  | Indoor CO₂ from Pi sensor (ppm)              |
| `indoorLoading`  | Boolean | True until first Pi response (loading pulse) |
| `sensorOnline`   | Boolean / null | null = connecting, true/false = status |
| `outdoorTemp`    | Number  | Outdoor temp from Open-Meteo (°C)            |
| `outdoorHumidity`| Number  | Outdoor humidity from Open-Meteo (%)         |
| `outdoorWind`    | Number  | Wind speed from Open-Meteo (km/h)            |
| `weatherCode`    | Number  | WMO code for current conditions              |
| `weatherDesc`    | String  | Plain text weather description               |
| `forecast`       | Array   | 5-day forecast: { day, temp, emoji }         |
| `lastUpdated`    | String  | Time string of last successful Pi fetch      |
| `justUpdated`    | Boolean | Briefly true after each Pi fetch (flash)     |
| `activeGraph`    | String  | Current period: 'day', 'week', or 'month'    |
| `activeMetric`   | String  | Current metric: 'temperature', 'humidity', 'co2' |
| `graphData`      | Object  | Placeholder graph data for all metrics/periods |

---

#### computed properties

**Status computeds** — return 'good', 'warning', 'bad', or 'no-data'
based on the current sensor reading and the defined healthy ranges:

| Computed         | Good range   | Warning range         | Bad (red)             |
|------------------|--------------|-----------------------|-----------------------|
| `tempStatus`     | 18–24°C      | 15–18°C or 24–27°C    | below 15 or above 27  |
| `humidityStatus` | 40–60%       | 30–40% or 60–70%      | below 30 or above 70  |
| `co2Status`      | below 800ppm | 800–1200 ppm          | above 1200 ppm        |

**Indicator position computeds** — return a 0–100 percentage used to
position the white dot on the scale bar. Returns -1 when no data (hides
the dot).

| Computed              | Scale                          |
|-----------------------|--------------------------------|
| `tempIndicatorPos`    | 10°C = 0%, 35°C = 100%         |
| `humidityIndicatorPos`| 0% = 0%, 100% = 100%           |
| `co2IndicatorPos`     | 300ppm = 0%, 2000ppm = 100%    |

**Sensor status computeds:**
- `sensorStatus` — returns 'connecting', 'online', or 'offline' from
  `sensorOnline`
- `sensorStatusText` — returns the matching display text

**Graph computeds:**
- `currentGraphData` — returns the correct dataset for the active metric
  and period combination
- `currentUnit` — returns the unit string (°C, %, ppm) for the active
  metric
- `weatherEmoji` — looks up the WMO emoji for the current weather code
- `graphAxisMax` — calculates a clean round y-axis maximum. Uses a
  metric-specific step size (5 for °C, 20 for %, 400 for ppm) and
  rounds up to the nearest step above the data maximum
- `yAxisTicks` — generates tick objects `{ label, y }` from 0 to
  graphAxisMax, spaced by the metric step. Used to draw grid lines and
  y-axis labels in the SVG
- `graphBars` — calculates bar geometry `{ x, y, barH, barWidth,
  labelX, labelY }` for every data point in the current view. Bars are
  scaled against graphAxisMax so they always fill the chart height

---

#### methods

**`fetchAll()`**
Calls both `fetchData()` and `fetchWeather()` together. Wired to the
"Refresh All" button.

**`fetchData()`**
Makes a GET request to the Pi sensor API at
`http://10.10.20.97:5000/klima-data`.

On success: sets `temperature`, `humidity`, `co2`, marks `sensorOnline`
as true, clears `indoorLoading`, and updates `lastUpdated`.

On failure: marks `sensorOnline` as false and clears `indoorLoading` so
the offline state is shown.

**`fetchWeather()`**
Makes a GET request to the Open-Meteo API URL.

On success: sets all outdoor data variables. The forecast is built by
skipping today (index 0) and taking the next 5 days. Dates are parsed
as local time (not UTC) to ensure the correct weekday name is shown in
the Copenhagen timezone.

**`statusText(status)`**
Helper that converts 'good' / 'warning' / 'bad' to the display strings
"Good" / "Warning" / "Poor" for the status badges.

---

#### mounted() / beforeUnmount()

On mount: immediately calls `fetchData()` and `fetchWeather()`, then
sets up two intervals:
- Indoor sensor: every **30 seconds**
- Outdoor weather: every **10 minutes**

On unmount: both intervals are cleared to avoid memory leaks.

---

### 3. stylesheet.css

All styles for the dashboard. No CSS framework is used.

**Key sections:**

| Section              | What it controls                                    |
|----------------------|-----------------------------------------------------|
| Global / body        | Dark blue gradient background, font, padding        |
| Title animations     | `@keyframes titleGlow` — slow glow pulse on h1      |
| Header               | Gradient text spans, decorative line, subtitle      |
| Indoor section       | Dark card, 3-column grid for metric tiles           |
| Sensor status        | Coloured dot + text, `loadingPulse` animation       |
| Metric tiles         | Flex column layout, info icon, tooltip popup        |
| Scale bar            | Gradient colour zones, white dot indicator          |
| Status badge         | Coloured pill with semi-transparent background      |
| Dashboard cards      | Flex column so content fills full card height       |
| Weather card         | `justify-content: space-between` pushes forecast    |
|                      | to bottom and current conditions to top             |
| Graph buttons        | Metric selector (Temperature/Humidity/CO₂) and      |
|                      | period selector (Day/Week/Month)                    |
| SVG chart            | Width 100%, height 150px                            |
| Responsive (768px)   | Single column layout for mobile screens             |

---

### 4. sensor_api.py (Raspberry Pi backend)

A Python Flask REST API that runs on the Raspberry Pi.

- Reads from the SCD30 CO₂ / temperature / humidity sensor over I2C
- Polls the sensor every 2 seconds in a background thread
- Exposes one endpoint: `GET /klima-data`
- Returns JSON: `{ "temperature": 21.4, "humidity": 55.2, "co2": 720 }`
- Runs on `0.0.0.0:5000` so it is reachable on the local network
- Uses Flask-CORS to allow the browser to fetch from it cross-origin

---

### 5. deploy_pi.py

A one-time deployment script run from a developer's PC.

- Connects to the Pi at `10.10.20.97` over SSH using Paramiko
- Enables the I2C interface on the Pi
- Installs required Python packages (Flask, Flask-CORS, scd30-i2c)
- Uploads `sensor_api.py` to `/home/zealand/` on the Pi via SFTP
- Creates and enables a systemd service so the API starts automatically
  on boot and restarts if it crashes

---

## Data Flow

```
Raspberry Pi (10.10.20.97)
  └── SCD30 sensor (I2C)
        └── sensor_api.py (Flask :5000)
              └── GET /klima-data
                    └── index.js fetchData() [every 30s]
                          └── Vue reactive data
                                └── HTML template renders

Open-Meteo API (internet)
  └── api.open-meteo.com/v1/forecast
        └── index.js fetchWeather() [every 10min]
              └── Vue reactive data
                    └── HTML template renders
```

---

## Colour Status Ranges

| Metric      | Green (Good)   | Orange (Warning)            | Red (Poor)              |
|-------------|----------------|-----------------------------|-------------------------|
| Temperature | 18–24°C        | 15–18°C or 24–27°C          | Below 15°C or above 27°C|
| Humidity    | 40–60%         | 30–40% or 60–70%            | Below 30% or above 70%  |
| CO₂         | Below 800 ppm  | 800–1200 ppm                | Above 1200 ppm          |

---

## Known Limitations

**Historical data is placeholder only.**
The graphs (Day / Week / Month) currently display hardcoded sample
numbers. The Pi backend only returns the current sensor reading — it
does not store readings over time. To make the graphs show real data,
the Pi would need to log each reading to a database or CSV file, and
the backend would need a new endpoint to return that history.

---

## Network Requirements

The dashboard must be opened on the same local network as the Raspberry
Pi for the indoor sensor data to load. The Pi is expected at:

```
http://10.10.20.97:5000/klima-data
```

Alternative IPs noted in the code: `192.168.14.57`

The outdoor weather (Open-Meteo) requires a regular internet connection
and works regardless of the Pi's availability.
