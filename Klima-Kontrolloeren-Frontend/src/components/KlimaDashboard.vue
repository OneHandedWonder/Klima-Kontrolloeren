<script setup>
import { ref, computed, onMounted, onBeforeUnmount } from 'vue'
import { useRouter } from 'vue-router'
import { signOut } from 'firebase/auth'
import axios from 'axios'
import { firebaseAuth } from '../firebase'

const router = useRouter()

// User authentication state
const currentUser = ref(null)
const userUID = ref(null)
const userSensors = ref([])
const authInitialized = ref(false)

// Comfort temperature settings
const comfortSettings = ref({
  min: 18,
  max: 24,
  warning: 3
})

// Comfort humidity settings
const comfortHumiditySettings = ref({
  min: 30,
  max: 60,
  warning: 10
})

const settingsModalOpen = ref(false)
const tempDangerConfirm = ref(false)
const humiditySettingsModalOpen = ref(false)
const humidityDangerConfirm = ref(false)
const settingsFormData = ref({
  tempMin: 18,
  tempMax: 24,
  humidityMin: 30,
  humidityMax: 60
})
const COMFORT_TEMP_KEY = 'klima:comfortTemp'
const COMFORT_HUMIDITY_KEY = 'klima:comfortHumidity'

function loadComfortTemp() {
  try {
    const saved = localStorage.getItem(COMFORT_TEMP_KEY)
    if (saved) {
      const parsed = JSON.parse(saved)
      comfortSettings.value = { ...comfortSettings.value, ...parsed }
      settingsFormData.value.tempMin = parsed.min
      settingsFormData.value.tempMax = parsed.max
    }
  } catch (e) {
    console.error('Failed to load comfort temperature settings:', e)
  }
}

function loadComfortHumidity() {
  try {
    const saved = localStorage.getItem(COMFORT_HUMIDITY_KEY)
    if (saved) {
      const parsed = JSON.parse(saved)
      comfortHumiditySettings.value = { ...comfortHumiditySettings.value, ...parsed }
      settingsFormData.value.humidityMin = parsed.min
      settingsFormData.value.humidityMax = parsed.max
    }
  } catch (e) {
    console.error('Failed to load comfort humidity settings:', e)
  }
}

function saveComfortTemp() {
  try {
    const toSave = {
      min: settingsFormData.value.tempMin,
      max: settingsFormData.value.tempMax,
      warning: 3
    }
    if (toSave.min < toSave.max) {
      comfortSettings.value = toSave
      localStorage.setItem(COMFORT_TEMP_KEY, JSON.stringify(toSave))
    }
  } catch (e) {
    console.error('Failed to save comfort temperature settings:', e)
  }
}

function saveComfortHumidity() {
  try {
    const toSave = {
      min: settingsFormData.value.humidityMin,
      max: settingsFormData.value.humidityMax,
      warning: 10
    }
    if (toSave.min < toSave.max) {
      comfortHumiditySettings.value = toSave
      localStorage.setItem(COMFORT_HUMIDITY_KEY, JSON.stringify(toSave))
    }
  } catch (e) {
    console.error('Failed to save comfort humidity settings:', e)
  }
}

function resetComfortTemp() {
  comfortSettings.value = { min: 18, max: 24, warning: 3 }
  settingsFormData.value.tempMin = 18
  settingsFormData.value.tempMax = 24
  try {
    localStorage.removeItem(COMFORT_TEMP_KEY)
  } catch (e) {
    console.error('Failed to reset comfort temperature settings:', e)
  }
}

function resetComfortHumidity() {
  comfortHumiditySettings.value = { min: 30, max: 60, warning: 10 }
  settingsFormData.value.humidityMin = 30
  settingsFormData.value.humidityMax = 60
  try {
    localStorage.removeItem(COMFORT_HUMIDITY_KEY)
  } catch (e) {
    console.error('Failed to reset comfort humidity settings:', e)
  }
}

function openSettingsModal() {
  settingsFormData.value = {
    tempMin: comfortSettings.value.min,
    tempMax: comfortSettings.value.max,
    humidityMin: comfortHumiditySettings.value.min,
    humidityMax: comfortHumiditySettings.value.max
  }
  settingsModalOpen.value = true
}

function closeSettingsModal() {
  settingsModalOpen.value = false
  tempDangerConfirm.value = false
}

const tempDangerWarnings = computed(() => {
  const warnings = []
  if (settingsFormData.value.tempMin < 16)
    warnings.push(`Minimum ${settingsFormData.value.tempMin}°C is below the recommended range — WHO recommends at least 16°C to avoid respiratory problems.`)
  if (settingsFormData.value.tempMax > 28)
    warnings.push(`Maximum ${settingsFormData.value.tempMax}°C is above the recommended range — above 28°C may cause heat stress and reduced productivity.`)
  return warnings
})

function handleSaveTemp() {
  if (tempDangerWarnings.value.length > 0) {
    tempDangerConfirm.value = true
  } else {
    saveComfortTemp()
    closeSettingsModal()
  }
}

function confirmSaveTemp() {
  tempDangerConfirm.value = false
  saveComfortTemp()
  closeSettingsModal()
}

function openHumiditySettingsModal() {
  settingsFormData.value.humidityMin = comfortHumiditySettings.value.min
  settingsFormData.value.humidityMax = comfortHumiditySettings.value.max
  humiditySettingsModalOpen.value = true
}

function closeHumiditySettingsModal() {
  humiditySettingsModalOpen.value = false
  humidityDangerConfirm.value = false
}

const humidityDangerWarnings = computed(() => {
  const warnings = []
  if (settingsFormData.value.humidityMin < 30)
    warnings.push(`Minimum ${settingsFormData.value.humidityMin}% is below the recommended range — may cause dry skin, static electricity and respiratory irritation.`)
  if (settingsFormData.value.humidityMax > 60)
    warnings.push(`Maximum ${settingsFormData.value.humidityMax}% is above the recommended range — may promote mould, dust mites and bacteria growth.`)
  return warnings
})

function handleSaveHumidity() {
  if (humidityDangerWarnings.value.length > 0) {
    humidityDangerConfirm.value = true
  } else {
    saveComfortHumidity()
    closeHumiditySettingsModal()
  }
}

function confirmSaveHumidity() {
  humidityDangerConfirm.value = false
  saveComfortHumidity()
  closeHumiditySettingsModal()
}

// Initialize user data from Firebase
async function initializeUser() {
  if (!firebaseAuth || !firebaseAuth.currentUser) {
    authInitialized.value = true
    return false
  }

  currentUser.value = firebaseAuth.currentUser
  const cachedUser = readCache(`user:${firebaseAuth.currentUser.uid}`, CACHE_TTL.user)

  if (cachedUser) {
    userUID.value = cachedUser.uid
    userSensors.value = cachedUser.sensors || []
    const isEnabled = await checkUserEnabled()
    if (!isEnabled) return false

    authInitialized.value = true
    return true
  }
  
  try {
    // Get ID token from Firebase user
    const idToken = await firebaseAuth.currentUser.getIdToken()
    
    // Call backend to get UID and sensors
    const response = await axios.get(
      `https://klimakontrolloeren-backend-b8h5g9azhqdjf3gm.norwayeast-01.azurewebsites.net/api/auth/getUserUID`,
      { params: { token: idToken } }
    )
    
    userUID.value = response.data.uid
    
    // Now fetch the user's sensors
    const sensorResponse = await axios.get(
      `https://klimakontrolloeren-backend-b8h5g9azhqdjf3gm.norwayeast-01.azurewebsites.net/api/sensor`,
      { params: { uid: userUID.value } }
    )
    
    // Check if user is enabled from sensor response
    if (Array.isArray(sensorResponse.data) && sensorResponse.data.length > 0) {
      const userData = sensorResponse.data[0]
      console.log('User data from sensor endpoint:', userData)
      console.log('enabled field:', userData.enabled)
      
      if (userData.enabled === false) {
        console.warn('User is disabled in the backend:', userUID.value)
        clearDashboardCache()
        // Sign out and redirect to sign-in page
        if (firebaseAuth) {
          try {
            await firebaseAuth.signOut()
            console.log('User signed out successfully')
          } catch (error) {
            console.error('Failed to sign out:', error)
          }
        }
        // Redirect to sign-in page
        router.push('/signin')
        return false
      }
      
      userSensors.value = userData.sensors || []
      writeCache(`user:${firebaseAuth.currentUser.uid}`, {
        uid: userUID.value,
        sensors: userSensors.value
      })
      console.log('User sensors loaded:', userSensors.value)
    }
  } catch (error) {
    console.error('Failed to initialize user data:', error.message)
  }
  
  authInitialized.value = true
  return true
}

async function handleSignOut() {
  if (!firebaseAuth) return
  await signOut(firebaseAuth)
  await router.push('/signin')
}

// Dynamic Open-Meteo URL builder — defaults to Roskilde
const weatherLat = ref(55.6415)
const weatherLon = ref(12.0803)
const selectedCity = ref('Roskilde, DK')

function buildOpenMeteoUrl(lat, lon) {
  const base = 'https://api.open-meteo.com/v1/forecast'
  const params = []
  params.push(`latitude=${encodeURIComponent(lat)}`)
  params.push(`longitude=${encodeURIComponent(lon)}`)
  params.push('current=temperature_2m,relative_humidity_2m,weather_code,wind_speed_10m')
  params.push('hourly=temperature_2m,relative_humidity_2m')
  params.push('daily=temperature_2m_max,weather_code')
  params.push('timezone=auto')
  params.push('forecast_days=6')
  params.push('past_days=31')
  return base + '?' + params.join('&')
}

// City search (Open-Meteo Geocoding)
const cityQuery = ref('')
const citySuggestions = ref([])
const searchingCity = ref(false)
let citySearchTimeout = null

function searchCity(query) {
  cityQuery.value = query
  citySuggestions.value = []
  if (citySearchTimeout) clearTimeout(citySearchTimeout)
  if (!query || query.length < 2) return
  citySearchTimeout = setTimeout(async () => {
    searchingCity.value = true
    try {
      const res = await axios.get('https://geocoding-api.open-meteo.com/v1/search?name=' + encodeURIComponent(query) + '&count=6')
      citySuggestions.value = (res.data && res.data.results) ? res.data.results.map(r => ({
        name: r.name + (r.admin1 ? ', ' + r.admin1 : '') + (r.country ? ', ' + r.country : ''),
        latitude: r.latitude,
        longitude: r.longitude,
        timezone: r.timezone
      })) : []
    } catch (e) {
      citySuggestions.value = []
    } finally {
      searchingCity.value = false
    }
  }, 300)
}

function selectCity(s) {
  if (!s) return
  selectedCity.value = s.name
  weatherLat.value = s.latitude
  weatherLon.value = s.longitude
  citySuggestions.value = []
  cityQuery.value = ''
  // Refresh weather for the new location
  fetchWeather(true)
}

// Favorites handling (stored in localStorage)
const favorites = ref([])
const FAVORITES_KEY = 'klima:favorites'

function loadFavorites() {
  try {
    const raw = localStorage.getItem(FAVORITES_KEY)
    if (!raw) return
    const parsed = JSON.parse(raw)
    if (Array.isArray(parsed)) favorites.value = parsed
  } catch (e) {
    // ignore
  }
}

function saveFavorites() {
  try {
    localStorage.setItem(FAVORITES_KEY, JSON.stringify(favorites.value))
  } catch (e) {
    // ignore
  }
}

function addFavorite() {
  // add currently selected city
  if (!selectedCity.value || !weatherLat.value || !weatherLon.value) return
  // avoid duplicates by name+coords
  const exists = favorites.value.find(f => f.name === selectedCity.value && f.lat === weatherLat.value && f.lon === weatherLon.value)
  if (exists) return
  favorites.value.push({ name: selectedCity.value, lat: weatherLat.value, lon: weatherLon.value })
  saveFavorites()
}

function removeFavorite(idx) {
  if (idx < 0 || idx >= favorites.value.length) return
  favorites.value.splice(idx, 1)
  saveFavorites()
}

function selectFavorite(fav) {
  if (!fav) return
  selectedCity.value = fav.name
  weatherLat.value = fav.lat
  weatherLon.value = fav.lon
  fetchWeather(true)
}

const isFavorited = computed(() => {
  return favorites.value.some(f => f.name === selectedCity.value && f.lat === weatherLat.value && f.lon === weatherLon.value)
})

function toggleFavorite() {
  const idx = favorites.value.findIndex(f => f.name === selectedCity.value && f.lat === weatherLat.value && f.lon === weatherLon.value)
  if (idx >= 0) {
    favorites.value.splice(idx, 1)
    saveFavorites()
    return
  }
  addFavorite()
}

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

const API_BASE = 'https://klimakontrolloeren-backend-b8h5g9azhqdjf3gm.norwayeast-01.azurewebsites.net/api/data'
const AVERAGE_ENDPOINTS = {
  hourly: `${API_BASE}/daylyAverage`,
  dayly: `${API_BASE}/weeklyAverage`,
  weekly: `${API_BASE}/monthlyAverage`
}

const CACHE_PREFIX = 'klima:dashboard:'
const CACHE_TTL = {
  user: 5 * 60 * 1000,
  sensorData: 60 * 1000,
  averageData: 10 * 60 * 1000,
  weather: 10 * 60 * 1000
}
const SENSOR_REFRESH_INTERVAL_MS = 60 * 1000
const WEATHER_REFRESH_INTERVAL_MS = 10 * 60 * 1000
const ENABLED_CHECK_INTERVAL_MS = 5 * 60 * 1000

function readCache(key, maxAgeMs) {
  try {
    const raw = localStorage.getItem(CACHE_PREFIX + key)
    if (!raw) return null

    const cached = JSON.parse(raw)
    if (!cached || Date.now() - cached.savedAt > maxAgeMs) return null

    return cached.value
  } catch (error) {
    console.warn('Failed to read dashboard cache:', error)
    return null
  }
}

function writeCache(key, value) {
  try {
    localStorage.setItem(CACHE_PREFIX + key, JSON.stringify({
      savedAt: Date.now(),
      value
    }))
  } catch (error) {
    console.warn('Failed to write dashboard cache:', error)
  }
}

function clearDashboardCache() {
  try {
    Object.keys(localStorage)
      .filter(key => key.startsWith(CACHE_PREFIX))
      .forEach(key => localStorage.removeItem(key))
  } catch (error) {
    console.warn('Failed to clear dashboard cache:', error)
  }
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
const pastWeatherReadings = ref({
  temperature: {
    hourly: [],
    daily: [],
    weekly: []
  },
  humidity: {
    hourly: [],
    daily: [],
    weekly: []
  }
})

const graphData = ref({
  temperature: { unit: '°C', hourly: [], dayly: [], weekly: [] },
  humidity: { unit: '%', hourly: [], dayly: [], weekly: [] },
  co2: { unit: 'ppm', hourly: [], dayly: [], weekly: [] }
})



const lastUpdated = ref(null)
const justUpdated = ref(false)
let refreshInterval = null
let weatherInterval = null
let enabledCheckInterval = null

const activeGraph = ref('hourly')
const activeMetric = ref('temperature')
const activePastMetric = ref('temperature')
const activeWeatherGraphPeriod = ref('hourly')

function formatGraphLabel(timePeriod, period, index) {
  if (!timePeriod) return String(index + 1)

  const raw = String(timePeriod)

  // Helper to parse incoming timestamp (assume UTC if no timezone provided)
  function parseUtcString(s) {
    if (/^\d{4}-\d{2}-\d{2}$/.test(s)) {
      return new Date(s + 'T00:00:00Z')
    }
    // if string already contains timezone info (Z or ±hh:mm), use as-is
    if (s.endsWith('Z') || /[+-]\d{2}:?\d{2}$/.test(s)) return new Date(s)
    // otherwise assume it's UTC local-formatted and append Z
    return new Date(s + 'Z')
  }

  try {
    const dt = parseUtcString(raw)
    if (Number.isNaN(dt.getTime())) return String(index + 1)

    if (period === 'hourly') {
      return dt.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })
    }

    if (period === 'dayly') {
      return new Intl.DateTimeFormat(undefined, { weekday: 'short' }).format(dt)
    }

    if (period === 'weekly') {
      // Show the week start date in local format, e.g. "12 May"
      return new Intl.DateTimeFormat(undefined, { month: 'short', day: 'numeric' }).format(dt)
    }

    return `Wk ${index + 1}`
  } catch (e) {
    return String(index + 1)
  }
}

function mapAverageSeries(rows, period, metricKey) {
  if (!Array.isArray(rows)) return []

  return rows.map((row, index) => {
    const rawTime = row.timePeriod ?? row.recordedAt ?? null
    const value = row[metricKey] ?? row[metricKey.replace(/^average/, '').charAt(0).toLowerCase() + metricKey.replace(/^average/, '').slice(1)] ?? null
    return {
      label: formatGraphLabel(rawTime, period, index),
      value,
      missing: value === null || value === undefined
    }
  })
}

const tempStatus = computed(() => {
  if (temperature.value === null) return 'no-data'
  const t = temperature.value
  const { min, max, warning } = comfortSettings.value
  if (t >= min && t <= max) return 'good'
  if ((t >= min - warning && t < min) || (t > max && t <= max + warning)) return 'warning'
  return 'bad'
})

const humidityStatus = computed(() => {
  if (humidity.value === null) return 'no-data'
  const h = humidity.value
  const { min, max, warning } = comfortHumiditySettings.value
  if (h >= min && h <= max) return 'good'
  if ((h >= min - warning && h < min) || (h > max && h <= max + warning)) return 'warning'
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
  const { min, max } = comfortSettings.value
  const rangeMin = min - 5
  const rangeMax = max + 10
  const range = rangeMax - rangeMin
  return Math.min(100, Math.max(0, ((temperature.value - rangeMin) / range) * 100))
})

const humidityIndicatorPos = computed(() => {
  if (humidity.value === null) return -1
  return Math.min(100, Math.max(0, humidity.value))
})

const co2IndicatorPos = computed(() => {
  if (co2.value === null) return -1
  return Math.min(100, Math.max(0, ((co2.value - 300) / 1700) * 100))
})

const currentGraphData = computed(() => graphData.value[activeMetric.value][activeGraph.value])
const currentUnit = computed(() => graphData.value[activeMetric.value].unit)
const weatherEmoji = computed(() => WMO_EMOJIS[weatherCode.value] || '🌡️')
const sensorStatus = computed(() => (sensorOnline.value === null ? 'connecting' : (sensorOnline.value ? 'online' : 'offline')))
const sensorStatusText = computed(() => (sensorOnline.value === null ? 'Connecting...' : (sensorOnline.value ? 'Sensor online' : 'Sensor offline')))
const mainHoveredIndex = ref(-1)
const pastHoveredIndex = ref(-1)


const climateAction = computed(() => {
  if (
    temperature.value === null ||
    humidity.value === null ||
    co2.value === null
  ) {
    return 'Waiting for sensor data...'
  }

  const { max, min, warning } = comfortSettings.value
  const tempAlarmHigh = max + warning
  const tempAlarmLow = min - warning

  // CO2 priority
  if (co2.value > 1200) {
    return '⚠️ Air quality is poor — opening ventilation / recommending window ventilation.'
  }

  // Temperature handling
  if (temperature.value > tempAlarmHigh) {
    return '🌡️ Room is too warm — activating cooling or increasing airflow.'
  }

  if (temperature.value < tempAlarmLow) {
    return '🥶 Room is too cold — activating heating.'
  }

  // Humidity handling
  if (humidity.value > 70) {
    return '💧 Humidity is too high — increasing ventilation to reduce moisture.'
  }

  if (humidity.value < 30) {
    return '🌵 Air is too dry — reducing ventilation or recommending humidification.'
  }

  return '✅ Indoor climate is stable — no automatic action needed.'
})

const climateActionStatus = computed(() => {
  if (temperature.value === null || humidity.value === null || co2.value === null) return 'neutral'
  const { max, min, warning } = comfortSettings.value
  const { max: humMax, min: humMin, warning: humWarning } = comfortHumiditySettings.value
  const tempAlarmHigh = max + warning
  const tempAlarmLow = min - warning
  const humAlarmHigh = humMax + humWarning
  const humAlarmLow = humMin - humWarning
  // Red: alarm thresholds breached
  if (co2.value > 1200 || temperature.value > tempAlarmHigh || temperature.value < tempAlarmLow || humidity.value > humAlarmHigh || humidity.value < humAlarmLow) return 'bad'
  // Orange: outside comfort range but not yet at alarm
  if (co2.value >= 800 || temperature.value > max || temperature.value < min || humidity.value > humMax || humidity.value < humMin) return 'warning'
  return 'good'
})




// Compute axis range (min/max) so negative values are supported correctly
const graphAxisRange = computed(() => {
  const data = currentGraphData.value
  if (!data.length) return { min: 0, max: 100, tickStep: 10 }
  // Exclude missing/null values (but allow zero and negatives)
  const values = data.map(d => d.value).filter(v => typeof v === 'number' && !Number.isNaN(v))
  if (!values.length) return { min: 0, max: 100, tickStep: 10 }
  const minVal = Math.min(...values)
  const maxVal = Math.max(...values)
  const tickStep = { temperature: 5, humidity: 20, co2: 400 }[activeMetric.value] || 10
  let minTick = Math.floor(minVal / tickStep) * tickStep
  let maxTick = Math.ceil(maxVal / tickStep) * tickStep
  if (minTick === maxTick) {
    // Provide a small range
    maxTick = minTick + tickStep
  }
  return { min: minTick, max: maxTick, tickStep }
})

const yAxisTicks = computed(() => {
  const data = currentGraphData.value
  if (!data.length) return []
  const { min, max, tickStep } = graphAxisRange.value
  const chartH = 118
  const topPad = 8
  const ticks = []
  for (let v = min; v <= max; v += tickStep) {
    const rel = (v - min) / (max - min)
    ticks.push({ label: v.toString(), y: topPad + chartH * (1 - rel) })
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
  const { min: axisMin, max: axisMax } = graphAxisRange.value
  const slotW = chartW / data.length
  const barW = slotW * 0.55
  return data.map((d, i) => {
    const value = typeof d.value === 'number' ? d.value : null
    const barH = (value !== null && axisMax > axisMin) ? ((value - axisMin) / (axisMax - axisMin)) * chartH : 0
    const maxLabels = activeGraph.value === 'weekly' ? 10 : 12
    const step = Math.max(1, Math.ceil(data.length / maxLabels))
    const showLabel = (i % step) === 0
    const rotate = data.length > maxLabels
    const status = d.missing ? 'no-data' : getStatusForValue(activeMetric.value, value)

    return {
      label: d.label,
      x: leftM + i * slotW + (slotW - barW) / 2,
      y: topPad + chartH - barH,
      barH,
      barWidth: barW,
      missing: d.missing,
      valueLabel: d.missing || value === null ? '—' : Number(value).toFixed(activeMetric.value === 'co2' ? 0 : 1),
      valueLabelY: topPad + chartH - barH - 4,
      showLabel,
      rotate,
      labelX: leftM + i * slotW + slotW / 2,
      labelY: topPad + chartH + 14,
      status,
      color: statusColors[status] || '#4CAF50'
    }
  })
})

function statusText(status) {
  if (status === 'good') return 'Good'
  if (status === 'warning') return 'Warning'
  if (status === 'bad') return 'Poor'
  return '—'
}

// Helper function to determine status for any value and metric type
function getStatusForValue(metric, value) {
  if (value === null || value === undefined || typeof value !== 'number' || Number.isNaN(value)) return 'no-data'

  if (metric === 'temperature') {
    const { min, max, warning } = comfortSettings.value
    if (value >= min && value <= max) return 'good'
    if ((value >= min - warning && value < min) || (value > max && value <= max + warning)) return 'warning'
    return 'bad'
  }

  if (metric === 'humidity') {
    const { min, max, warning } = comfortHumiditySettings.value
    if (value >= min && value <= max) return 'good'
    if ((value >= min - warning && value < min) || (value > max && value <= max + warning)) return 'warning'
    return 'bad'
  }

  if (metric === 'co2') {
    if (value < 800) return 'good'
    if (value <= 1200) return 'warning'
    return 'bad'
  }

  return 'no-data'
}

// Color mapping for status
const statusColors = {
  good: '#4CAF50',
  warning: '#FF9800',
  bad: '#f44336',
  'no-data': 'rgba(255,255,255,0.18)'
}

// Dynamic temperature scale bar gradient
const tempScaleGradient = computed(() => {
  const { min, max, warning } = comfortSettings.value
  const rangeMin = min - 5
  const rangeMax = max + 10
  const range = rangeMax - rangeMin

  // Calculate percentage positions for each threshold
  const p_bad_min = 0
  const p_warn_min = ((min - warning - rangeMin) / range) * 100
  const p_good_min = ((min - rangeMin) / range) * 100
  const p_good_max = ((max - rangeMin) / range) * 100
  const p_warn_max = ((max + warning - rangeMin) / range) * 100
  const p_bad_max = 100

  return `linear-gradient(to right, #f44336 ${p_bad_min}%, #f44336 ${p_warn_min}%, #FF9800 ${p_warn_min}%, #FF9800 ${p_good_min}%, #4CAF50 ${p_good_min}%, #4CAF50 ${p_good_max}%, #FF9800 ${p_good_max}%, #FF9800 ${p_warn_max}%, #f44336 ${p_warn_max}%, #f44336 ${p_bad_max}%)`
})

const humidityScaleGradient = computed(() => {
  const { min, max, warning } = comfortHumiditySettings.value

  // Calculate percentage positions for each threshold (0-100% scale)
  const p_bad_min = 0
  const p_warn_min = Math.max(0, min - warning)
  const p_good_min = min
  const p_good_max = max
  const p_warn_max = Math.min(100, max + warning)
  const p_bad_max = 100

  return `linear-gradient(to right, #f44336 ${p_bad_min}%, #f44336 ${p_warn_min}%, #FF9800 ${p_warn_min}%, #FF9800 ${p_good_min}%, #4CAF50 ${p_good_min}%, #4CAF50 ${p_good_max}%, #FF9800 ${p_good_max}%, #FF9800 ${p_warn_max}%, #f44336 ${p_warn_max}%, #f44336 ${p_bad_max}%)`
})

const tempScaleRangeLabel = computed(() => {
  const { min, max } = comfortSettings.value
  return `${min}–${max}°C`
})

const pastGraphData = computed(() => pastWeatherReadings.value[activePastMetric.value][activeWeatherGraphPeriod.value] || [])
const pastGraphUnit = computed(() => {
  if (activePastMetric.value === 'temperature') return '°C'
  if (activePastMetric.value === 'humidity') return '%'
  return ''
})

// Compute past graph axis range supporting negatives
const pastGraphAxisRange = computed(() => {
  const data = pastGraphData.value
  if (!data.length) return { min: 0, max: 100, tickStep: 10 }
  const values = data.map(d => d.value).filter(v => typeof v === 'number' && !Number.isNaN(v))
  if (!values.length) return { min: 0, max: 100, tickStep: 10 }
  const minVal = Math.min(...values)
  const maxVal = Math.max(...values)
  const tickStep = { temperature: 5, humidity: 20 }[activePastMetric.value] || 10
  let minTick = Math.floor(minVal / tickStep) * tickStep
  let maxTick = Math.ceil(maxVal / tickStep) * tickStep
  if (minTick === maxTick) maxTick = minTick + tickStep
  return { min: minTick, max: maxTick, tickStep }
})

const pastGraphYAxisTicks = computed(() => {
  const data = pastGraphData.value
  if (!data.length) return []
  const { min, max, tickStep } = pastGraphAxisRange.value
  const chartH = 100
  const topPad = 8
  const ticks = []
  for (let v = min; v <= max; v += tickStep) {
    const rel = (v - min) / (max - min)
    ticks.push({ label: v.toString(), y: topPad + chartH * (1 - rel) })
  }
  return ticks
})

const pastZeroLineY = computed(() => {
  const { min: axisMin, max: axisMax } = pastGraphAxisRange.value
  const axisRange = axisMax - axisMin
  if (axisRange === 0) return null
  const zeroFrac = (0 - axisMin) / axisRange
  if (zeroFrac <= 0 || zeroFrac >= 1) return null
  return 8 + 100 * (1 - zeroFrac)
})

const pastGraphBars = computed(() => {
  const data = pastGraphData.value
  if (!data.length) return []
  const leftM = 30
  const chartW = 262
  const chartH = 100
  const topPad = 8
  const { min: axisMin, max: axisMax } = pastGraphAxisRange.value
  const slotW = chartW / data.length
  const barW = slotW * 0.55

  const maxLabels = activeWeatherGraphPeriod.value === 'weekly' ? 10 : 12
  const step = Math.max(1, Math.ceil(data.length / maxLabels))

  const axisRange = axisMax - axisMin
  const zeroFrac = axisRange > 0 ? Math.max(0, Math.min(1, (0 - axisMin) / axisRange)) : 0
  const zeroY = topPad + chartH * (1 - zeroFrac)

  return data.map((d, i) => {
    const value = typeof d.value === 'number' ? d.value : null
    let barH = 0
    let y = zeroY

    if (value !== null && axisRange > 0) {
      const frac = (value - axisMin) / axisRange
      const valueY = topPad + chartH * (1 - frac)
      barH = Math.max(1, Math.abs(zeroY - valueY))
      y = Math.min(zeroY, valueY)
    }

    const showLabel = (i % step) === 0
    const rotate = data.length > maxLabels

    return {
      label: d.label,
      x: leftM + i * slotW + (slotW - barW) / 2,
      y,
      barH,
      barWidth: barW,
      missing: d.missing,
      valueLabel: d.missing || value === null ? '—' : Number(value).toFixed(1),
      valueLabelY: y - 4,
      showLabel,
      rotate,
      labelX: leftM + i * slotW + slotW / 2,
      labelY: topPad + chartH + 14
    }
  })
})

async function fetchAverageData(force = false) {
  const cacheKey = `average:${userUID.value || 'all'}`
  const cached = force ? null : readCache(cacheKey, CACHE_TTL.averageData)
  if (cached) {
    graphData.value = cached
    return
  }

  try {
    const params = userUID.value ? { uid: userUID.value } : {}
    
    const [hourlyResponse, daylyResponse, weeklyResponse] = await Promise.all([
      axios.get(AVERAGE_ENDPOINTS.hourly, { params }),
      axios.get(AVERAGE_ENDPOINTS.dayly, { params }),
      axios.get(AVERAGE_ENDPOINTS.weekly, { params })
    ])

    graphData.value = {
      temperature: {
        unit: '°C',
        hourly: mapAverageSeries(hourlyResponse.data, 'hourly', 'averageTemperature'),
        dayly: mapAverageSeries(daylyResponse.data, 'dayly', 'averageTemperature'),
        weekly: mapAverageSeries(weeklyResponse.data, 'weekly', 'averageTemperature')
      },
      humidity: {
        unit: '%',
        hourly: mapAverageSeries(hourlyResponse.data, 'hourly', 'averageHumidity'),
        dayly: mapAverageSeries(daylyResponse.data, 'dayly', 'averageHumidity'),
        weekly: mapAverageSeries(weeklyResponse.data, 'weekly', 'averageHumidity')
      },
      co2: {
        unit: 'ppm',
        hourly: mapAverageSeries(hourlyResponse.data, 'hourly', 'averageCO2PPM'),
        dayly: mapAverageSeries(daylyResponse.data, 'dayly', 'averageCO2PPM'),
        weekly: mapAverageSeries(weeklyResponse.data, 'weekly', 'averageCO2PPM')
      }
    }
    // Ensure the weekly series represents the last 30 days exactly.
    function ensureLastN(arr, n) {
      if (!Array.isArray(arr)) return Array(n).fill({ label: '', value: null, missing: true })
      if (arr.length >= n) return arr.slice(-n)
      const pad = Array(n - arr.length).fill({ label: '', value: null, missing: true })
      return pad.concat(arr)
    }

    graphData.value.temperature.weekly = ensureLastN(graphData.value.temperature.weekly, 30)
    graphData.value.humidity.weekly = ensureLastN(graphData.value.humidity.weekly, 30)
    graphData.value.co2.weekly = ensureLastN(graphData.value.co2.weekly, 30)
    writeCache(cacheKey, graphData.value)
  } catch (error) {
    console.error('Average data fetch error:', error.message)
  }
}

async function checkUserEnabled() {
  if (!userUID.value) return true
  
  try {
    const sensorResponse = await axios.get(
      `https://klimakontrolloeren-backend-b8h5g9azhqdjf3gm.norwayeast-01.azurewebsites.net/api/sensor`,
      { params: { uid: userUID.value } }
    )
    
    if (Array.isArray(sensorResponse.data) && sensorResponse.data.length > 0) {
      const userData = sensorResponse.data[0]
      
      if (userData.enabled === false) {
        console.warn('User was disabled while logged in:', userUID.value)
        clearDashboardCache()
        // Sign out and redirect
        if (firebaseAuth) {
          try {
            await firebaseAuth.signOut()
          } catch (error) {
            console.error('Failed to sign out:', error)
          }
        }
        router.push('/signin')
        return false
      }

      userSensors.value = userData.sensors || userSensors.value
      if (firebaseAuth?.currentUser) {
        writeCache(`user:${firebaseAuth.currentUser.uid}`, {
          uid: userUID.value,
          sensors: userSensors.value
        })
      }
    }

    return true
  } catch (error) {
    console.error('Failed to check user enabled status:', error.message)
    return true
  }
}

function fetchData(force = false) {
  const cacheKey = `sensor-data:${userUID.value || 'all'}`
  const cached = force ? null : readCache(cacheKey, CACHE_TTL.sensorData)
  if (cached) {
    applySensorData(cached)
    return
  }

  const endpoint = userUID.value ? `${API_BASE}?uid=${userUID.value}` : API_BASE
  
  axios.get(endpoint)
    .then(response => {
      console.log('Indoor sensor full response:', response.data)
      // Handle array response — take the latest (first) reading
      const data = Array.isArray(response.data) ? response.data[0] : response.data
      writeCache(cacheKey, data)
      applySensorData(data)
    })
    .catch(error => {
      console.error('Indoor sensor fetch error:', error.message, error.response?.data)
      sensorOnline.value = false
      indoorLoading.value = false
    })
}

function applySensorData(data) {
  if (!data) {
    sensorOnline.value = false
    indoorLoading.value = false
    return
  }

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
}

function fetchWeather(force = false) {
  const cacheKey = `weather:${weatherLat.value}:${weatherLon.value}`
  const cached = force ? null : readCache(cacheKey, CACHE_TTL.weather)
  if (cached) {
    applyWeatherData(cached)
    return
  }

  axios.get(buildOpenMeteoUrl(weatherLat.value, weatherLon.value))
    .then(response => {
      writeCache(cacheKey, response.data)
      applyWeatherData(response.data)
    })
    .catch(err => {
      console.error('Weather fetch error:', err)
    })
}

function applyWeatherData(weatherData) {
  const current = weatherData.current
  outdoorTemp.value = current.temperature_2m
  outdoorHumidity.value = current.relative_humidity_2m
  outdoorWind.value = Math.round(current.wind_speed_10m)
  weatherCode.value = current.weather_code
  weatherDesc.value = WMO_DESCRIPTIONS[current.weather_code] || '—'

  const daily = weatherData.daily
  const dayNames = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat']
  forecast.value = daily.time.slice(1, 6).map((dateStr, i) => {
    const [y, m, d] = dateStr.split('-').map(Number)
    const localDate = new Date(y, m - 1, d)
    return { day: dayNames[localDate.getDay()], temp: Math.round(daily.temperature_2m_max[i + 1]), emoji: WMO_EMOJIS[daily.weather_code[i + 1]] || '🌡️' }
  })

  // Process hourly data for 24-hour period
  if (weatherData.hourly && weatherData.hourly.time) {
    const hourly = weatherData.hourly
    const times = hourly.time
    const temps = hourly.temperature_2m
    const humidities = hourly.relative_humidity_2m
    
    // Get the last 24 hours of data
    const last24Index = Math.max(0, times.length - 24)
    const last24Times = times.slice(last24Index)
    const last24Temps = temps.slice(last24Index)
    const last24Humidities = humidities.slice(last24Index)
    
    pastWeatherReadings.value.temperature.hourly = last24Times.map((timeStr, idx) => {
      const dt = new Date(timeStr + 'Z')
      const label = dt.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })
      return {
        label,
        value: last24Temps[idx],
        missing: last24Temps[idx] === null || last24Temps[idx] === undefined
      }
    })
    
    pastWeatherReadings.value.humidity.hourly = last24Times.map((timeStr, idx) => {
      const dt = new Date(timeStr + 'Z')
      const label = dt.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })
      return {
        label,
        value: last24Humidities[idx],
        missing: last24Humidities[idx] === null || last24Humidities[idx] === undefined
      }
    })

    // Calculate daily aggregates from hourly data for 7 and 30 day periods
    const dailyAggregates = {}
    times.forEach((timeStr, idx) => {
      const date = new Date(timeStr + 'Z')
      const dateKey = date.toISOString().split('T')[0]
      
      if (!dailyAggregates[dateKey]) {
        dailyAggregates[dateKey] = {
          temps: [],
          humidities: [],
          date: dateKey
        }
      }
      
      if (temps[idx] !== null && temps[idx] !== undefined) {
        dailyAggregates[dateKey].temps.push(temps[idx])
      }
      if (humidities[idx] !== null && humidities[idx] !== undefined) {
        dailyAggregates[dateKey].humidities.push(humidities[idx])
      }
    })

    // Convert to array and sort by date
    const sortedDates = Object.values(dailyAggregates)
      .sort((a, b) => new Date(a.date) - new Date(b.date))

    // 7-day data
    const last7Days = sortedDates.slice(-7)
    pastWeatherReadings.value.temperature.daily = last7Days.map((day) => {
      const dt = new Date(day.date + 'T00:00:00Z')
      const label = new Intl.DateTimeFormat(undefined, { weekday: 'short' }).format(dt)
      const avgTemp = day.temps.length > 0 ? day.temps.reduce((a, b) => a + b) / day.temps.length : null
      return {
        label,
        value: avgTemp,
        missing: avgTemp === null
      }
    })
    
    pastWeatherReadings.value.humidity.daily = last7Days.map((day) => {
      const dt = new Date(day.date + 'T00:00:00Z')
      const label = new Intl.DateTimeFormat(undefined, { weekday: 'short' }).format(dt)
      const avgHumidity = day.humidities.length > 0 ? day.humidities.reduce((a, b) => a + b) / day.humidities.length : null
      return {
        label,
        value: avgHumidity,
        missing: avgHumidity === null
      }
    })

    // 30-day data
    const last30Days = sortedDates.slice(-30)
    pastWeatherReadings.value.temperature.weekly = last30Days.map((day) => {
      const dt = new Date(day.date + 'T00:00:00Z')
      const label = new Intl.DateTimeFormat(undefined, { month: 'short', day: 'numeric' }).format(dt)
      const avgTemp = day.temps.length > 0 ? day.temps.reduce((a, b) => a + b) / day.temps.length : null
      return {
        label,
        value: avgTemp,
        missing: avgTemp === null
      }
    })
    
    pastWeatherReadings.value.humidity.weekly = last30Days.map((day) => {
      const dt = new Date(day.date + 'T00:00:00Z')
      const label = new Intl.DateTimeFormat(undefined, { month: 'short', day: 'numeric' }).format(dt)
      const avgHumidity = day.humidities.length > 0 ? day.humidities.reduce((a, b) => a + b) / day.humidities.length : null
      return {
        label,
        value: avgHumidity,
        missing: avgHumidity === null
      }
    })
  }
}

function fetchAll(force = false) {
  fetchData(force)
  fetchWeather(force)
  fetchAverageData(force)
}

onMounted(() => {
  loadFavorites()
  loadComfortTemp()
  loadComfortHumidity()
  // Initialize user and sensors first, then fetch data
  initializeUser().then((isEnabled) => {
    if (!isEnabled) return

    fetchAll()
    refreshInterval = setInterval(fetchData, SENSOR_REFRESH_INTERVAL_MS)
    weatherInterval = setInterval(fetchWeather, WEATHER_REFRESH_INTERVAL_MS)
    enabledCheckInterval = setInterval(checkUserEnabled, ENABLED_CHECK_INTERVAL_MS)
  })
})

onBeforeUnmount(() => {
  clearInterval(refreshInterval)
  clearInterval(weatherInterval)
  clearInterval(enabledCheckInterval)
})
</script>

<template>
  <div v-if="!authInitialized" class="auth-loading-container">
    <p>Initializing user data...</p>
  </div>
  <div v-else class="container">
    <header class="header">
      <div class="header-center">
        <h1><span class="title-klima">Klima</span><span class="title-dash">-</span><span class="title-kontrol">Kontrolloeren</span></h1>
        <p class="header-sub">— Indoor Climate Monitor —</p>
      </div>
      <div class="header-right">
        <div v-if="currentUser" class="user-info">
          <div class="user-summary">
            <div class="user-avatar">{{ currentUser.email?.charAt(0).toUpperCase() || '?' }}</div>
            <div class="user-details">
              <p class="user-email">{{ currentUser.email }}</p>
            </div>
          </div>
          <div class="user-actions">
            <router-link to="/profile" class="profile-button" title="Account settings">⚙️</router-link>
            <button class="sign-out-btn" @click="handleSignOut" title="Sign out">Sign out</button>
          </div>
        </div>
      </div>
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
                <div class="tooltip-range good">● Good — {{ comfortSettings.min }}–{{ comfortSettings.max }}°C: Comfortable room temperature</div>
                <div class="tooltip-range warning">● Warning — {{ comfortSettings.min - comfortSettings.warning }}–{{ comfortSettings.min }}°C or {{ comfortSettings.max }}–{{ comfortSettings.max + comfortSettings.warning }}°C: Slightly cold or warm</div>
                <div class="tooltip-range bad">● Poor — below {{ comfortSettings.min - comfortSettings.warning }}°C or above {{ comfortSettings.max + comfortSettings.warning }}°C: Too cold or too hot</div>
              </div>
            </span>
          </div>
          <span :class="['metric-value', tempStatus]">{{ temperature !== null ? temperature : '—' }}<span class="metric-unit">°C</span></span>
          <div class="status-scale">
            <div class="scale-bar" :style="{ background: tempScaleGradient }">
              <span v-if="tempIndicatorPos >= 0" class="scale-indicator" :style="{ left: tempIndicatorPos + '%' }"></span>
            </div>
            <div class="scale-range-labels"><span>{{ comfortSettings.min - 5 }}°C</span><span>{{ tempScaleRangeLabel }}</span><span>{{ comfortSettings.max + 10 }}°C</span></div>
            <span v-if="tempStatus !== 'no-data'" :class="['status-badge', tempStatus]">{{ statusText(tempStatus) }}</span>
          </div>
          <button class="settings-btn" @click="openSettingsModal" title="Adjust temperature preferences">⚙️ Adjust Preferences</button>
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
            <div class="scale-bar" :style="{ background: humidityScaleGradient }">
              <span v-if="humidityIndicatorPos >= 0" class="scale-indicator" :style="{ left: humidityIndicatorPos + '%' }"></span>
            </div>
            <div class="scale-range-labels"><span>0%</span><span>{{ comfortHumiditySettings.min }}–{{ comfortHumiditySettings.max }}%</span><span>100%</span></div>
            <span v-if="humidityStatus !== 'no-data'" :class="['status-badge', humidityStatus]">{{ statusText(humidityStatus) }}</span>
          </div>
          <button class="settings-btn" @click="openHumiditySettingsModal" title="Adjust humidity preferences">⚙️ Adjust Preferences</button>
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

    <!-- System feedback bar -->
    <div :class="['system-feedback-bar', climateActionStatus]">
      <h3>Automatic Climate System</h3>
      <p class="system-feedback-text">{{ climateAction }}</p>
    </div>

    <!-- Outdoor + graphs -->
    <div class="dashboard-container">
      <div class="dashboard-column">
        <div class="dashboard-card weather-card">
          <h3>Outdoor Weather</h3>
            <div class="card-content weather-layout">
              <div class="weather-top-row">
                <div class="weather-main-panel">
                  <p class="subtitle">{{ selectedCity }}</p>
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

                  <div class="forecast">
                    <div class="forecast-day" v-for="day in forecast" :key="day.day">
                      <span class="forecast-name">{{ day.day }}</span>
                      <span class="forecast-emoji">{{ day.emoji }}</span>
                      <span class="forecast-temp">{{ day.temp }}°</span>
                    </div>
                  </div>
                </div>

                <aside class="search-card">
                  <div class="search-card-header">
                    <div class="search-card-title">Favorites</div>
                    <button class="fav-toggle" :class="{ active: isFavorited }" @click="toggleFavorite" aria-label="Toggle favorite">{{ isFavorited ? '★' : '☆' }}</button>
                  </div>
                  <div class="city-search">
                    <input
                      v-model="cityQuery"
                      @input="searchCity(cityQuery)"
                      placeholder="Search city..."
                      class="city-input"
                    />
                    <ul v-if="citySuggestions.length" class="suggestions">
                      <li v-for="(s, i) in citySuggestions" :key="s.name + i" @click="selectCity(s)">{{ s.name }}</li>
                    </ul>
                  </div>
                  <div class="favorites-panel">
                    <div class="fav-header"></div>
                    <div class="favorites-list" :class="{ scrollable: favorites.length > 5 }">
                      <button v-for="(f, idx) in favorites" :key="f.name + idx" class="fav-item" @click="selectFavorite(f)">
                        <span class="fav-name">{{ f.name }}</span>
                        <span class="fav-remove" @click.stop="removeFavorite(idx)">✕</span>
                      </button>
                      <div v-if="!favorites.length" class="favorites-empty">No favorites yet.</div>
                    </div>
                  </div>
                </aside>
              </div>
            </div>
        </div>

        <div class="dashboard-card past-readings-card">
          <h3>Past Readings — {{ selectedCity }}</h3>
          <p class="subtitle">Outdoor weather history</p>
          <div class="past-graph-metric-btns">
            <button @click="activePastMetric = 'temperature'" :class="['metric-graph-btn', { active: activePastMetric === 'temperature' }]">Temperature</button>
            <button @click="activePastMetric = 'humidity'" :class="['metric-graph-btn', { active: activePastMetric === 'humidity' }]">Humidity</button>
          </div>
          <div class="past-graph-period-btns">
            <button @click="activeWeatherGraphPeriod = 'hourly'" :class="['period-btn', { active: activeWeatherGraphPeriod === 'hourly' }]">Daily</button>
            <button @click="activeWeatherGraphPeriod = 'daily'" :class="['period-btn', { active: activeWeatherGraphPeriod === 'daily' }]">Weekly</button>
            <button @click="activeWeatherGraphPeriod = 'weekly'" :class="['period-btn', { active: activeWeatherGraphPeriod === 'weekly' }]">Monthly</button>
          </div>
          <div class="chart-placeholder">
            <svg viewBox="0 0 300 140" class="bar-chart past-bar-chart">
              <g v-for="tick in pastGraphYAxisTicks" :key="tick.label">
                <line x1="30" :y1="tick.y" x2="292" :y2="tick.y" stroke="rgba(255,255,255,0.07)" stroke-width="1"/>
                <text x="27" :y="tick.y + 3" text-anchor="end" fill="#5a7f99" font-size="7">{{ tick.label }}</text>
              </g>
              <text x="30" y="5" text-anchor="middle" fill="#5a7f99" font-size="7">{{ pastGraphUnit }}</text>
              <line x1="30" y1="8" x2="30" y2="118" stroke="rgba(255,255,255,0.12)" stroke-width="1"/>
              <line v-if="pastZeroLineY !== null" x1="30" :y1="pastZeroLineY" x2="292" :y2="pastZeroLineY" stroke="rgba(255,255,255,0.35)" stroke-width="1" stroke-dasharray="3,2"/>
              <g v-for="(bar, idx) in pastGraphBars" :key="bar.label">
                <rect
                    :x="bar.x"
                    :y="bar.y"
                    :width="bar.barWidth"
                    :height="bar.barH"
                    :fill="bar.missing ? 'rgba(255,255,255,0.18)' : '#4CAF50'"
                    :opacity="bar.missing ? 0.45 : 1"
                    rx="2"
                    @mouseenter="pastHoveredIndex = idx"
                    @mouseleave="pastHoveredIndex = -1"
                  />
                  <text v-if="pastHoveredIndex === idx" :x="bar.labelX" :y="bar.valueLabelY" text-anchor="middle" fill="#d8f0ff" font-size="7">{{ bar.valueLabel }}</text>
                <text v-if="bar.showLabel"
                      :x="bar.labelX"
                      :y="bar.labelY"
                      :transform="bar.rotate ? `rotate(-45 ${bar.labelX} ${bar.labelY})` : undefined"
                      text-anchor="middle"
                      fill="#7fa8bf"
                      :font-size="bar.rotate ? 6.5 : 7.5">
                  {{ bar.label }}
                </text>
              </g>
            </svg>
          </div>
        </div>
      </div>

      <div class="dashboard-card graphs-card">
        <h3>Graphs</h3>
        <p class="subtitle">Historical graphs</p>
        <div class="graph-metric-btns">
          <button @click="activeMetric = 'temperature'" :class="['metric-graph-btn', { active: activeMetric === 'temperature' }]">Temperature</button>
          <button @click="activeMetric = 'humidity'" :class="['metric-graph-btn', { active: activeMetric === 'humidity' }]">Humidity</button>
          <button @click="activeMetric = 'co2'" :class="['metric-graph-btn', { active: activeMetric === 'co2' }]">CO₂</button>
        </div>
        <div class="graph-period-btns">
          <button @click="activeGraph = 'hourly'" :class="['period-btn', { active: activeGraph === 'hourly' }]">Daily</button>
          <button @click="activeGraph = 'dayly'" :class="['period-btn', { active: activeGraph === 'dayly' }]">Weekly</button>
          <button @click="activeGraph = 'weekly'" :class="['period-btn', { active: activeGraph === 'weekly' }]">Monthly</button>
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
              <g v-for="(bar, idx) in graphBars" :key="bar.label">
                <rect
                  :x="bar.x"
                  :y="bar.y"
                  :width="bar.barWidth"
                  :height="bar.barH"
                  :fill="bar.color"
                  :opacity="bar.missing ? 0.45 : 1"
                  rx="2"
                  @mouseenter="mainHoveredIndex = idx"
                  @mouseleave="mainHoveredIndex = -1"
                />
                <text v-if="mainHoveredIndex === idx" :x="bar.labelX" :y="bar.valueLabelY" text-anchor="middle" fill="#d8f0ff" font-size="7.5">{{ bar.valueLabel }}</text>
                <text v-if="bar.showLabel"
                      :x="bar.labelX"
                      :y="bar.labelY"
                      :transform="bar.rotate ? `rotate(-45 ${bar.labelX} ${bar.labelY})` : undefined"
                      text-anchor="middle"
                      fill="#7fa8bf"
                      :font-size="bar.rotate ? 7 : 8">
                  {{ bar.label }}
                </text>
              </g>
            </svg>
          </div>
        </div>
      </div>
    </div>

    <div class="button-section">
      <button @click="fetchAll(true)" class="refresh-btn">Refresh All</button>
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

    <!-- Settings Modal -->
    <div v-if="settingsModalOpen" class="settings-modal-overlay" @click="closeSettingsModal">
      <div class="settings-modal" @click.stop>
        <div class="modal-header">
          <h2>Temperature Preferences</h2>
          <button class="modal-close-btn" @click="closeSettingsModal">✕</button>
        </div>
        <div class="modal-body">
          <p class="modal-description">Set your comfortable temperature range. The visual indicators will update to match your preferences.</p>

          <!-- Recommended range info -->
          <div class="humidity-recommendation">
            <p>
              <strong>Recommended indoor temperature: 18–24°C</strong><br>
              Below 16°C may cause respiratory problems (WHO). Above 28°C may cause heat stress and reduced productivity.
            </p>
          </div>

          <!-- Danger warning (live) -->
          <div v-if="tempDangerWarnings.length > 0 && !tempDangerConfirm" class="humidity-danger-warning">
            <p v-for="w in tempDangerWarnings" :key="w">⚠️ {{ w }}</p>
          </div>

          <!-- Confirmation step -->
          <div v-if="tempDangerConfirm" class="humidity-danger-confirm">
            <p>⚠️ <strong>Are you sure?</strong> The values you entered are outside the recommended safe range:</p>
            <ul>
              <li v-for="w in tempDangerWarnings" :key="w">{{ w }}</li>
            </ul>
            <p>Saving these values may result in an unhealthy indoor environment.</p>
            <div class="confirm-btn-group">
              <button class="btn-danger" @click="confirmSaveTemp">Yes, save anyway</button>
              <button class="btn-cancel" @click="tempDangerConfirm = false">Go back</button>
            </div>
          </div>

          <!-- Form (hidden during confirmation) -->
          <template v-if="!tempDangerConfirm">
            <div class="settings-section">
              <h3 class="section-title">Temperature (°C)</h3>
              <div class="form-group">
                <label for="min-temp">Minimum Comfortable Temperature</label>
                <input id="min-temp" v-model.number="settingsFormData.tempMin" type="number" min="5" max="35" step="0.5" class="form-input"/>
              </div>
              <div class="form-group">
                <label for="max-temp">Maximum Comfortable Temperature</label>
                <input id="max-temp" v-model.number="settingsFormData.tempMax" type="number" min="5" max="35" step="0.5" class="form-input"/>
              </div>
              <div class="settings-preview">
                <p class="preview-label">Preview:</p>
                <div class="preview-info">
                  <span class="preview-range">Good: {{ settingsFormData.tempMin }}–{{ settingsFormData.tempMax }}°C</span>
                  <span class="preview-warning">Warning zones: {{ settingsFormData.tempMin - 3 }}–{{ settingsFormData.tempMin }}°C or {{ settingsFormData.tempMax }}–{{ settingsFormData.tempMax + 3 }}°C</span>
                  <span class="preview-danger">Danger zones: below {{ settingsFormData.tempMin - 3 }}°C or above {{ settingsFormData.tempMax + 3 }}°C</span>
                </div>
              </div>
            </div>
          </template>
        </div>

        <div v-if="!tempDangerConfirm" class="modal-footer">
          <button class="btn-secondary" @click="resetComfortTemp">Reset to Defaults</button>
          <div class="button-group">
            <button class="btn-cancel" @click="closeSettingsModal">Cancel</button>
            <button class="btn-primary" @click="handleSaveTemp">Save Changes</button>
          </div>
        </div>
      </div>
    </div>

    <!-- Humidity Settings Modal -->
    <div v-if="humiditySettingsModalOpen" class="settings-modal-overlay" @click="closeHumiditySettingsModal">
      <div class="settings-modal" @click.stop>
        <div class="modal-header">
          <h2>Humidity Preferences</h2>
          <button class="modal-close-btn" @click="closeHumiditySettingsModal">✕</button>
        </div>
        <div class="modal-body">
          <p class="modal-description">Set your comfortable humidity range. The visual indicators will update to match your preferences.</p>

          <!-- Recommended range info -->
          <div class="humidity-recommendation">
            <p>
              <strong>Recommended indoor humidity: 30–60%</strong><br>
              Below 30% can cause dry skin and respiratory irritation. Above 60% may promote mould and dust mites.
            </p>
            <a href="https://www.mayoclinic.org/diseases-conditions/common-cold/in-depth/humidifiers/art-20048021#main-content" target="_blank" rel="noopener" class="humidity-source-link">
              📖 Mayo Clinic — Humidifiers and Indoor Humidity
            </a>
            <a href="https://awgeurope.com/what-humidity-level-is-good-for-your-body/" target="_blank" rel="noopener" class="humidity-source-link">
              📖 AWG Europe — What Humidity Level Is Good for Your Body?
            </a>
          </div>

          <!-- Danger warning (live) -->
          <div v-if="humidityDangerWarnings.length > 0 && !humidityDangerConfirm" class="humidity-danger-warning">
            <p v-for="w in humidityDangerWarnings" :key="w">⚠️ {{ w }}</p>
          </div>

          <!-- Confirmation step -->
          <div v-if="humidityDangerConfirm" class="humidity-danger-confirm">
            <p>⚠️ <strong>Are you sure?</strong> The values you entered are outside the recommended safe range:</p>
            <ul>
              <li v-for="w in humidityDangerWarnings" :key="w">{{ w }}</li>
            </ul>
            <p>Saving these values may result in an unhealthy indoor environment.</p>
            <div class="confirm-btn-group">
              <button class="btn-danger" @click="confirmSaveHumidity">Yes, save anyway</button>
              <button class="btn-cancel" @click="humidityDangerConfirm = false">Go back</button>
            </div>
          </div>

          <!-- Form (hidden during confirmation) -->
          <template v-if="!humidityDangerConfirm">
            <div class="settings-section">
              <h3 class="section-title">Humidity (%)</h3>
              <div class="form-group">
                <label for="min-humidity">Minimum Comfortable Humidity</label>
                <input id="min-humidity" v-model.number="settingsFormData.humidityMin" type="number" min="0" max="100" step="1" class="form-input"/>
              </div>
              <div class="form-group">
                <label for="max-humidity">Maximum Comfortable Humidity</label>
                <input id="max-humidity" v-model.number="settingsFormData.humidityMax" type="number" min="0" max="100" step="1" class="form-input"/>
              </div>
              <div class="settings-preview">
                <p class="preview-label">Preview:</p>
                <div class="preview-info">
                  <span class="preview-range">Good: {{ settingsFormData.humidityMin }}–{{ settingsFormData.humidityMax }}%</span>
                  <span class="preview-warning">Warning zones: {{ settingsFormData.humidityMin - 10 }}–{{ settingsFormData.humidityMin }}% or {{ settingsFormData.humidityMax }}–{{ settingsFormData.humidityMax + 10 }}%</span>
                  <span class="preview-danger">Danger zones: below {{ settingsFormData.humidityMin - 10 }}% or above {{ settingsFormData.humidityMax + 10 }}%</span>
                </div>
              </div>
            </div>
          </template>
        </div>

        <div v-if="!humidityDangerConfirm" class="modal-footer">
          <button class="btn-secondary" @click="resetComfortHumidity">Reset to Defaults</button>
          <div class="button-group">
            <button class="btn-cancel" @click="closeHumiditySettingsModal">Cancel</button>
            <button class="btn-primary" @click="handleSaveHumidity">Save Changes</button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>
