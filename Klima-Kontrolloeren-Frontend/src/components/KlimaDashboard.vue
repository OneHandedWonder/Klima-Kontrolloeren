<script setup>
import { ref, computed, onMounted, onBeforeUnmount } from 'vue'
import axios from 'axios'

const OPEN_METEO_URL = 'https://api.open-meteo.com/v1/forecast' +
  '?latitude=55.6415&longitude=12.0803' +
  '&current=temperature_2m,relative_humidity_2m,weather_code,wind_speed_10m' +
  '&daily=temperature_2m_max,weather_code' +
  '&timezone=Europe%2FCopenhagen' +
  '&forecast_days=6'

const WMO_DESCRIPTIONS = {
  0: 'Clear sky',
  1: 'Mainly clear', 2: 'Partly cloudy', 3: 'Overcast',
  45: 'Fog', 48: 'Icy fog',
  51: 'Light drizzle', 53: 'Drizzle', 55: 'Heavy drizzle',
  61: 'Light rain', 63: 'Rain', 65: 'Heavy rain',
  71: 'Light snow', 73: 'Snow', 75: 'Heavy snow', 77: 'Snow grains',
  80: 'Light showers', 81: 'Showers', 82: 'Heavy showers',
  85: 'Snow showers', 86: 'Heavy snow showers',
  95: 'Thunderstorm', 96: 'Thunderstorm with hail', 99: 'Thunderstorm with hail'
}

const WMO_EMOJIS = {
  0: '☀️',
  1: '🌤️', 2: '⛅', 3: '☁️',
  45: '🌫️', 48: '🌫️',
  51: '🌦️', 53: '🌦️', 55: '🌧️',
  61: '🌧️', 63: '🌧️', 65: '🌧️',
  71: '❄️', 73: '❄️', 75: '❄️', 77: '🌨️',
  80: '🌦️', 81: '🌦️', 82: '🌧️',
  85: '🌨️', 86: '🌨️',
  95: '⛈️', 96: '⛈️', 99: '⛈️'
}

// Reactive state — fetched from actual API
const temperature = ref(null)
const humidity = ref(null)
const co2 = ref(null)
const indoorLoading = ref(true)
const sensorOnline = ref(null)

const outdoorTemp = ref(null)
const outdoorHumidity = ref(null)
const outdoorWind = ref(null)
const weatherCode = ref(null)
const weatherDesc = ref('—')
const forecast = ref([])

const lastUpdated = ref(null)
const justUpdated = ref(false)
let refreshInterval = null
let weatherInterval = null

const activeGraph = ref('day')
const activeMetric = ref('temperature')

const graphData = {
  temperature: {
    unit: '°C',
    day: [
      { label: '00:00', value: 19.8 }, { label: '03:00', value: 19.2 },
      { label: '06:00', value: 19.5 }, { label: '09:00', value: 21.0 },
      { label: '12:00', value: 22.4 }, { label: '15:00', value: 23.1 },
      { label: '18:00', value: 22.8 }, { label: '21:00', value: 21.3 }
    ],
    week: [
      { label: 'Mon', value: 21.2 }, { label: 'Tue', value: 22.0 },
      { label: 'Wed', value: 20.5 }, { label: 'Thu', value: 23.1 },
      { label: 'Fri', value: 22.7 }, { label: 'Sat', value: 21.8 },
      { label: 'Sun', value: 20.9 }
    ],
    month: [
      { label: 'Wk 1', value: 21.5 }, { label: 'Wk 2', value: 22.1 },
      { label: 'Wk 3', value: 20.8 }, { label: 'Wk 4', value: 21.9 }
    ]
  },
  humidity: {
    unit: '%',
    day: [
      { label: '00:00', value: 48 }, { label: '03:00', value: 46 },
      { label: '06:00', value: 50 }, { label: '09:00', value: 55 },
      { label: '12:00', value: 58 }, { label: '15:00', value: 62 },
      { label: '18:00', value: 65 }, { label: '21:00', value: 54 }
    ],
    week: [
      { label: 'Mon', value: 54 }, { label: 'Tue', value: 60 },
      { label: 'Wed', value: 49 }, { label: 'Thu', value: 63 },
      { label: 'Fri', value: 58 }, { label: 'Sat', value: 52 },
      { label: 'Sun', value: 47 }
    ],
    month: [
      { label: 'Wk 1', value: 55 }, { label: 'Wk 2', value: 61 },
      { label: 'Wk 3', value: 50 }, { label: 'Wk 4', value: 57 }
    ]
  },
  co2: {
    unit: 'ppm',
    day: [
      { label: '00:00', value: 520 }, { label: '03:00', value: 490 },
      { label: '06:00', value: 540 }, { label: '09:00', value: 720 },
      { label: '12:00', value: 950 }, { label: '15:00', value: 880 },
      { label: '18:00', value: 1050 }, { label: '21:00', value: 760 }
    ],
    week: [
      { label: 'Mon', value: 750 }, { label: 'Tue', value: 820 },
      { label: 'Wed', value: 680 }, { label: 'Thu', value: 910 },
      { label: 'Fri', value: 870 }, { label: 'Sat', value: 620 },
      { label: 'Sun', value: 580 }
    ],
    month: [
      { label: 'Wk 1', value: 720 }, { label: 'Wk 2', value: 810 },
      { label: 'Wk 3', value: 650 }, { label: 'Wk 4', value: 790 }
    ]
  }
}

const tempStatus = computed(() => {
  if (temperature.value === null) return 'no-data'
  const t = temperature.value
  if (t >= 18 && t <= 24) return 'good'
  if ((t >= 15 && t < 18) || (t > 24 && t <= 27)) return 'warning'
  return 'bad'
})

const humidityStatus = computed(() => {
  if (humidity.value === null) return 'no-data'
  const h = humidity.value
  if (h >= 40 && h <= 60) return 'good'
  if ((h >= 30 && h < 40) || (h > 60 && h <= 70)) return 'warning'
  return 'bad'
})

const co2Status = computed(() => {
  if (co2.value === null) return 'no-data'
  if (co2.value < 800) return 'good'
  if (co2.value <= 1200) return 'warning'
  return 'bad'
})

const tempIndicatorPos = computed(() => {
  if (temperature.value === null) return -1
  return Math.min(100, Math.max(0, ((temperature.value - 10) / 25) * 100))
})

const humidityIndicatorPos = computed(() => {
  if (humidity.value === null) return -1
  return Math.min(100, Math.max(0, humidity.value))
})

const co2IndicatorPos = computed(() => {
  if (co2.value === null) return -1
  return Math.min(100, Math.max(0, ((co2.value - 300) / 1700) * 100))
})

const currentGraphData = computed(() => graphData[activeMetric.value][activeGraph.value])
const currentUnit = computed(() => graphData[activeMetric.value].unit)
const weatherEmoji = computed(() => WMO_EMOJIS[weatherCode.value] || '🌡️')
const sensorStatus = computed(() => (sensorOnline.value === null ? 'connecting' : (sensorOnline.value ? 'online' : 'offline')))
const sensorStatusText = computed(() => (sensorOnline.value === null ? 'Connecting...' : (sensorOnline.value ? 'Sensor online' : 'Sensor offline')))

const graphAxisMax = computed(() => {
  const data = currentGraphData.value
  if (!data.length) return 100
  const maxVal = Math.max(...data.map(d => d.value))
  const tickStep = { temperature: 5, humidity: 20, co2: 400 }[activeMetric.value] || 10
  return Math.ceil(maxVal / tickStep) * tickStep
})

const yAxisTicks = computed(() => {
  const data = currentGraphData.value
  if (!data.length) return []
  const axisMax = graphAxisMax.value
  const tickStep = { temperature: 5, humidity: 20, co2: 400 }[activeMetric.value] || 10
  const chartH = 118
  const topPad = 8
  const ticks = []
  for (let v = 0; v <= axisMax; v += tickStep) {
    ticks.push({ label: v.toString(), y: topPad + chartH * (1 - v / axisMax) })
  }
  return ticks
})

const graphBars = computed(() => {
  const data = currentGraphData.value
  if (!data.length) return []
  const leftM = 30
  const chartW = 262
  const chartH = 118
  const topPad = 8
  const axisMax = graphAxisMax.value
  const slotW = chartW / data.length
  const barW = slotW * 0.55
  return data.map((d, i) => {
    const barH = (d.value / axisMax) * chartH
    return {
      label: d.label,
      x: leftM + i * slotW + (slotW - barW) / 2,
      y: topPad + chartH - barH,
      barH,
      barWidth: barW,
      labelX: leftM + i * slotW + slotW / 2,
      labelY: topPad + chartH + 14
    }
  })
})

function statusText(status) {
  if (status === 'good') return 'Good'
  if (status === 'warning') return 'Warning'
  if (status === 'bad') return 'Poor'
  return '—'
}

function fetchData() {
  axios.get('https://klimakontrolloeren-backend-b8h5g9azhqdjf3gm.norwayeast-01.azurewebsites.net/api/sensor')
    .then(response => {
      console.log('Indoor sensor full response:', response.data)
      // Handle array response — take the latest (first) reading
      const data = Array.isArray(response.data) ? response.data[0] : response.data
      console.log('Sensor data object keys:', Object.keys(data))
      console.log('Sensor data object:', data)
      
      temperature.value = data.temperature
      humidity.value = data.humidity
      // CO2 field is named cO2PPM in the API
      co2.value = data.cO2PPM || null
      
      sensorOnline.value = true
      indoorLoading.value = false
      lastUpdated.value = new Date().toLocaleTimeString()
      justUpdated.value = true
      setTimeout(() => { justUpdated.value = false }, 2000)
    })
    .catch(error => {
      console.error('Indoor sensor fetch error:', error.message, error.response?.data)
      sensorOnline.value = false
      indoorLoading.value = false
    })
}

function fetchWeather() {
  axios.get(OPEN_METEO_URL)
    .then(response => {
      const current = response.data.current
      outdoorTemp.value = current.temperature_2m
      outdoorHumidity.value = current.relative_humidity_2m
      outdoorWind.value = Math.round(current.wind_speed_10m)
      weatherCode.value = current.weather_code
      weatherDesc.value = WMO_DESCRIPTIONS[current.weather_code] || '—'

      const daily = response.data.daily
      const dayNames = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat']
      forecast.value = daily.time.slice(1, 6).map((dateStr, i) => {
        const [y, m, d] = dateStr.split('-').map(Number)
        const localDate = new Date(y, m - 1, d)
        return { day: dayNames[localDate.getDay()], temp: Math.round(daily.temperature_2m_max[i + 1]), emoji: WMO_EMOJIS[daily.weather_code[i + 1]] || '🌡️' }
      })
    })
    .catch(() => {})
}

function fetchAll() {
  fetchData()
  fetchWeather()
}

onMounted(() => {
  fetchData()
  fetchWeather()
  refreshInterval = setInterval(fetchData, 30000)
  weatherInterval = setInterval(fetchWeather, 600000)
})

onBeforeUnmount(() => {
  clearInterval(refreshInterval)
  clearInterval(weatherInterval)
})
</script>

<template>
  <div class="container">
    <header class="header">
      <h1><span class="title-klima">Klima</span><span class="title-dash">-</span><span class="title-kontrol">Kontrolloeren</span></h1>
      <p class="header-sub">— Indoor Climate Monitor —</p>
    </header>

    <!-- Indoor main display (template body adapted from legacy HTML) -->
    <div class="indoor-section">
      <h2 class="indoor-title">Indoor Sensor Data</h2>
      <div class="sensor-status">
        <span :class="['status-dot', sensorStatus]"></span>
        <span class="status-text">{{ sensorStatusText }}</span>
      </div>
      <div :class="['indoor-metrics', { loading: indoorLoading }]">
        <!-- Temperature tile -->
        <div class="metric-tile">
          <div class="metric-header">
            <span class="metric-label">Temperature</span>
            <span class="info-wrap">
              <span class="info-icon">i</span>
              <div class="info-tooltip">
                <strong>Temperature</strong>
                <p>Indoor air temperature measured by the Pi sensor.</p>
                <div class="tooltip-range good">● Good — 18–24°C: Comfortable room temperature</div>
                <div class="tooltip-range warning">● Warning — 15–18°C or 24–27°C: Slightly cold or warm</div>
                <div class="tooltip-range bad">● Poor — below 15°C or above 27°C: Too cold or too hot</div>
              </div>
            </span>
          </div>
          <span :class="['metric-value', tempStatus]">{{ temperature !== null ? temperature : '—' }}<span class="metric-unit">°C</span></span>
          <div class="status-scale">
            <div class="scale-bar" style="background: linear-gradient(to right, #f44336 0%, #f44336 20%, #FF9800 20%, #FF9800 32%, #4CAF50 32%, #4CAF50 56%, #FF9800 56%, #FF9800 68%, #f44336 68%, #f44336 100%);">
              <span v-if="tempIndicatorPos >= 0" class="scale-indicator" :style="{ left: tempIndicatorPos + '%' }"></span>
            </div>
            <div class="scale-range-labels"><span>10°C</span><span>18–24°C</span><span>35°C</span></div>
            <span v-if="tempStatus !== 'no-data'" :class="['status-badge', tempStatus]">{{ statusText(tempStatus) }}</span>
          </div>
        </div>

        <!-- Humidity tile -->
        <div class="metric-tile">
          <div class="metric-header">
            <span class="metric-label">Humidity</span>
            <span class="info-wrap">
              <span class="info-icon">i</span>
              <div class="info-tooltip">
                <strong>Humidity</strong>
                <p>Relative indoor humidity measured by the Pi sensor.</p>
                <div class="tooltip-range good">● Good — 40–60%: Ideal for health &amp; comfort</div>
                <div class="tooltip-range warning">● Warning — 30–40% or 60–70%: Getting dry or humid</div>
                <div class="tooltip-range bad">● Poor — below 30% or above 70%: Risk of dryness or mould</div>
              </div>
            </span>
          </div>
          <span :class="['metric-value', humidityStatus]">{{ humidity !== null ? humidity : '—' }}<span class="metric-unit">%</span></span>
          <div class="status-scale">
            <div class="scale-bar" style="background: linear-gradient(to right, #f44336 0%, #f44336 30%, #FF9800 30%, #FF9800 40%, #4CAF50 40%, #4CAF50 60%, #FF9800 60%, #FF9800 70%, #f44336 70%, #f44336 100%);">
              <span v-if="humidityIndicatorPos >= 0" class="scale-indicator" :style="{ left: humidityIndicatorPos + '%' }"></span>
            </div>
            <div class="scale-range-labels"><span>0%</span><span>40–60%</span><span>100%</span></div>
            <span v-if="humidityStatus !== 'no-data'" :class="['status-badge', humidityStatus]">{{ statusText(humidityStatus) }}</span>
          </div>
        </div>

        <!-- CO2 tile -->
        <div class="metric-tile">
          <div class="metric-header">
            <span class="metric-label">CO₂</span>
            <span class="info-wrap">
              <span class="info-icon">i</span>
              <div class="info-tooltip">
                <strong>CO₂</strong>
                <p>Carbon dioxide concentration measured by the Pi sensor.</p>
                <div class="tooltip-range good">● Good — below 800 ppm: Fresh, excellent air quality</div>
                <div class="tooltip-range warning">● Warning — 800–1200 ppm: Acceptable, ventilate soon</div>
                <div class="tooltip-range bad">● Poor — above 1200 ppm: Bad air quality, open a window!</div>
              </div>
            </span>
          </div>
          <span :class="['metric-value', co2Status]">{{ co2 !== null ? co2 : '—' }}<span class="metric-unit">ppm</span></span>
          <div class="status-scale">
            <div class="scale-bar" style="background: linear-gradient(to right, #4CAF50 0%, #4CAF50 29%, #FF9800 29%, #FF9800 53%, #f44336 53%, #f44336 100%);">
              <span v-if="co2IndicatorPos >= 0" class="scale-indicator" :style="{ left: co2IndicatorPos + '%' }"></span>
            </div>
            <div class="scale-range-labels"><span>300</span><span>&lt;800 ppm</span><span>2000</span></div>
            <span v-if="co2Status !== 'no-data'" :class="['status-badge', co2Status]">{{ statusText(co2Status) }}</span>
          </div>
        </div>
      </div>
    </div>

    <!-- Outdoor + graphs -->
    <div class="dashboard-container">
      <div class="dashboard-card weather-card">
        <h3>Outdoor Weather</h3>
        <p class="subtitle">Roskilde, DK — open-meteo.com</p>
        <div class="card-content">
          <div class="weather-current">
            <div class="weather-main">
              <span class="weather-emoji">{{ weatherEmoji }}</span>
              <span class="outdoor-temp">{{ outdoorTemp !== null ? outdoorTemp : '—' }}°C</span>
            </div>
            <p class="weather-desc">{{ weatherDesc }}</p>
            <div class="weather-details">
              <span class="weather-detail">💧 {{ outdoorHumidity !== null ? outdoorHumidity : '—' }}%</span>
              <span class="weather-detail">💨 {{ outdoorWind !== null ? outdoorWind : '—' }} km/h</span>
            </div>
          </div>

          <div class="weather-divider"></div>

          <div class="forecast">
            <div class="forecast-day" v-for="day in forecast" :key="day.day">
              <span class="forecast-name">{{ day.day }}</span>
              <span class="forecast-emoji">{{ day.emoji }}</span>
              <span class="forecast-temp">{{ day.temp }}°</span>
            </div>
          </div>
        </div>
      </div>

      <div class="dashboard-card">
        <h3>Graphs</h3>
        <p class="subtitle">Historical graphs</p>
        <div class="graph-metric-btns">
          <button @click="activeMetric = 'temperature'" :class="['metric-graph-btn', { active: activeMetric === 'temperature' }]">Temperature</button>
          <button @click="activeMetric = 'humidity'" :class="['metric-graph-btn', { active: activeMetric === 'humidity' }]">Humidity</button>
          <button @click="activeMetric = 'co2'" :class="['metric-graph-btn', { active: activeMetric === 'co2' }]">CO₂</button>
        </div>
        <div class="graph-period-btns">
          <button @click="activeGraph = 'day'" :class="['period-btn', { active: activeGraph === 'day' }]">Day</button>
          <button @click="activeGraph = 'week'" :class="['period-btn', { active: activeGraph === 'week' }]">Week</button>
          <button @click="activeGraph = 'month'" :class="['period-btn', { active: activeGraph === 'month' }]">Month</button>
        </div>
        <div class="card-content">
          <div class="chart-placeholder">
            <svg viewBox="0 0 300 150" class="bar-chart">
              <g v-for="tick in yAxisTicks" :key="tick.label">
                <line x1="30" :y1="tick.y" x2="292" :y2="tick.y" stroke="rgba(255,255,255,0.07)" stroke-width="1"/>
                <text x="27" :y="tick.y + 3" text-anchor="end" fill="#5a7f99" font-size="7.5">{{ tick.label }}</text>
              </g>
              <text x="30" y="5" text-anchor="middle" fill="#5a7f99" font-size="7">{{ currentUnit }}</text>
              <line x1="30" y1="8" x2="30" y2="126" stroke="rgba(255,255,255,0.12)" stroke-width="1"/>
              <g v-for="bar in graphBars" :key="bar.label">
                <rect :x="bar.x" :y="bar.y" :width="bar.barWidth" :height="bar.barH" fill="#4CAF50" rx="2"/>
                <text :x="bar.labelX" :y="bar.labelY" text-anchor="middle" fill="#7fa8bf" font-size="8">{{ bar.label }}</text>
              </g>
            </svg>
          </div>
        </div>
      </div>
    </div>

    <div class="button-section">
      <button @click="fetchAll" class="refresh-btn">Refresh All</button>
      <div class="update-status">
        <span v-if="lastUpdated" :class="['update-time', { 'just-updated': justUpdated }]">
          {{ justUpdated ? 'Sensor updated!' : 'Sensor updated: ' + lastUpdated }}
        </span>
        <span v-else class="update-time">Waiting for data...</span>
      </div>
    </div>

    <footer class="footer">
      <p>Klima-Kontrolloeren · Indoor Climate Monitor · Zealand 3. Semester Systemudvikling</p>
    </footer>
  </div>
</template>

<style scoped>
/* Component-level tweaks can go here; main styles loaded globally. */
</style>
