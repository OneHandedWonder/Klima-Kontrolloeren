// Roskilde, DK — no API key needed
const OPEN_METEO_URL = 'https://api.open-meteo.com/v1/forecast' +
    '?latitude=55.6415&longitude=12.0803' +
    '&current=temperature_2m,relative_humidity_2m,weather_code,wind_speed_10m' +
    '&daily=temperature_2m_max,weather_code' +
    '&timezone=Europe%2FCopenhagen' +
    '&forecast_days=6';

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
};

const WMO_EMOJIS = {
    0: '☀️',
    1: '🌤️', 2: '⛅', 3: '☁️',
    45: '🌫️', 48: '🌫️',
    51: '🌦️', 53: '🌦️', 55: '🌧️',
    61: '🌧️', 63: '🌧️', 65: '🌧️',
    71: '❄️',  73: '❄️',  75: '❄️',  77: '🌨️',
    80: '🌦️', 81: '🌦️', 82: '🌧️',
    85: '🌨️', 86: '🌨️',
    95: '⛈️', 96: '⛈️', 99: '⛈️'
};

const { createApp } = Vue;

createApp({
    data() {
        return {
            // Indoor — Pi sensor
            temperature: null,
            humidity: null,
            co2: null,
            indoorLoading: true,
            sensorOnline: null,

            // Outdoor — Open-Meteo
            outdoorTemp: null,
            outdoorHumidity: null,
            outdoorWind: null,
            weatherCode: null,
            weatherDesc: '—',
            forecast: [],

            lastUpdated: null,
            justUpdated: false,
            refreshInterval: null,
            weatherInterval: null,

            activeGraph:  'day',
            activeMetric: 'temperature',
            graphData: {
                temperature: {
                    unit: '°C',
                    day:   [
                        { label: '00:00', value: 19.8 }, { label: '03:00', value: 19.2 },
                        { label: '06:00', value: 19.5 }, { label: '09:00', value: 21.0 },
                        { label: '12:00', value: 22.4 }, { label: '15:00', value: 23.1 },
                        { label: '18:00', value: 22.8 }, { label: '21:00', value: 21.3 },
                    ],
                    week:  [
                        { label: 'Mon', value: 21.2 }, { label: 'Tue', value: 22.0 },
                        { label: 'Wed', value: 20.5 }, { label: 'Thu', value: 23.1 },
                        { label: 'Fri', value: 22.7 }, { label: 'Sat', value: 21.8 },
                        { label: 'Sun', value: 20.9 },
                    ],
                    month: [
                        { label: 'Wk 1', value: 21.5 }, { label: 'Wk 2', value: 22.1 },
                        { label: 'Wk 3', value: 20.8 }, { label: 'Wk 4', value: 21.9 },
                    ]
                },
                humidity: {
                    unit: '%',
                    day:   [
                        { label: '00:00', value: 48 }, { label: '03:00', value: 46 },
                        { label: '06:00', value: 50 }, { label: '09:00', value: 55 },
                        { label: '12:00', value: 58 }, { label: '15:00', value: 62 },
                        { label: '18:00', value: 65 }, { label: '21:00', value: 54 },
                    ],
                    week:  [
                        { label: 'Mon', value: 54 }, { label: 'Tue', value: 60 },
                        { label: 'Wed', value: 49 }, { label: 'Thu', value: 63 },
                        { label: 'Fri', value: 58 }, { label: 'Sat', value: 52 },
                        { label: 'Sun', value: 47 },
                    ],
                    month: [
                        { label: 'Wk 1', value: 55 }, { label: 'Wk 2', value: 61 },
                        { label: 'Wk 3', value: 50 }, { label: 'Wk 4', value: 57 },
                    ]
                },
                co2: {
                    unit: 'ppm',
                    day:   [
                        { label: '00:00', value: 520 }, { label: '03:00', value: 490 },
                        { label: '06:00', value: 540 }, { label: '09:00', value: 720 },
                        { label: '12:00', value: 950 }, { label: '15:00', value: 880 },
                        { label: '18:00', value: 1050 }, { label: '21:00', value: 760 },
                    ],
                    week:  [
                        { label: 'Mon', value: 750 }, { label: 'Tue', value: 820 },
                        { label: 'Wed', value: 680 }, { label: 'Thu', value: 910 },
                        { label: 'Fri', value: 870 }, { label: 'Sat', value: 620 },
                        { label: 'Sun', value: 580 },
                    ],
                    month: [
                        { label: 'Wk 1', value: 720 }, { label: 'Wk 2', value: 810 },
                        { label: 'Wk 3', value: 650 }, { label: 'Wk 4', value: 790 },
                    ]
                }
            }
        };
    },
    computed: {
        // Temperature: green 18-24°C, orange 15-18 / 24-27, red outside
        tempStatus() {
            if (this.temperature === null) return 'no-data';
            const t = this.temperature;
            if (t >= 18 && t <= 24) return 'good';
            if ((t >= 15 && t < 18) || (t > 24 && t <= 27)) return 'warning';
            return 'bad';
        },
        // Humidity: green 40-60%, orange 30-40 / 60-70, red outside
        humidityStatus() {
            if (this.humidity === null) return 'no-data';
            const h = this.humidity;
            if (h >= 40 && h <= 60) return 'good';
            if ((h >= 30 && h < 40) || (h > 60 && h <= 70)) return 'warning';
            return 'bad';
        },
        // CO2: green <800ppm, orange 800-1200, red >1200
        co2Status() {
            if (this.co2 === null) return 'no-data';
            if (this.co2 < 800) return 'good';
            if (this.co2 <= 1200) return 'warning';
            return 'bad';
        },
        // Indicator positions (%) on the scale bar
        tempIndicatorPos() {
            if (this.temperature === null) return -1;
            return Math.min(100, Math.max(0, ((this.temperature - 10) / 25) * 100));
        },
        humidityIndicatorPos() {
            if (this.humidity === null) return -1;
            return Math.min(100, Math.max(0, this.humidity));
        },
        co2IndicatorPos() {
            if (this.co2 === null) return -1;
            return Math.min(100, Math.max(0, ((this.co2 - 300) / 1700) * 100));
        },
        currentGraphData() {
            return this.graphData[this.activeMetric][this.activeGraph];
        },
        currentUnit() {
            return this.graphData[this.activeMetric].unit;
        },
        weatherEmoji() {
            return WMO_EMOJIS[this.weatherCode] || '🌡️';
        },
        sensorStatus() {
            if (this.sensorOnline === null) return 'connecting';
            return this.sensorOnline ? 'online' : 'offline';
        },
        sensorStatusText() {
            if (this.sensorOnline === null) return 'Connecting...';
            return this.sensorOnline ? 'Sensor online' : 'Sensor offline';
        },
        // Shared y-axis max: rounds up to nearest "nice" step per metric
        graphAxisMax() {
            const data = this.currentGraphData;
            if (!data.length) return 100;
            const maxVal   = Math.max(...data.map(d => d.value));
            const tickStep = { temperature: 5, humidity: 20, co2: 400 }[this.activeMetric] || 10;
            return Math.ceil(maxVal / tickStep) * tickStep;
        },
        yAxisTicks() {
            const data     = this.currentGraphData;
            if (!data.length) return [];
            const axisMax  = this.graphAxisMax;
            const tickStep = { temperature: 5, humidity: 20, co2: 400 }[this.activeMetric] || 10;
            const chartH   = 118;
            const topPad   = 8;
            const ticks    = [];
            for (let v = 0; v <= axisMax; v += tickStep) {
                ticks.push({
                    label: v.toString(),
                    y:     topPad + chartH * (1 - v / axisMax)
                });
            }
            return ticks;
        },
        graphBars() {
            const data    = this.currentGraphData;
            if (!data.length) return [];
            const leftM   = 30;
            const chartW  = 262; // 292 - 30
            const chartH  = 118;
            const topPad  = 8;
            const axisMax = this.graphAxisMax;
            const slotW   = chartW / data.length;
            const barW    = slotW * 0.55;

            return data.map((d, i) => {
                const barH = (d.value / axisMax) * chartH;
                return {
                    label:    d.label,
                    x:        leftM + i * slotW + (slotW - barW) / 2,
                    y:        topPad + chartH - barH,
                    barH:     barH,
                    barWidth: barW,
                    labelX:   leftM + i * slotW + slotW / 2,
                    labelY:   topPad + chartH + 14
                };
            });
        }
    },
    methods: {
        statusText(status) {
            if (status === 'good') return 'Good';
            if (status === 'warning') return 'Warning';
            if (status === 'bad') return 'Poor';
            return '—';
        },

        fetchAll() {
            this.fetchData();
            this.fetchWeather();
        },

        // Indoor: temperature, humidity, CO2 from the Pi sensor API
        fetchData() {
            axios.get('http://10.10.20.97:5000/klima-data') // 192.168.14.57 or home 10.10.20.97:5000
                .then(response => {
                    this.temperature  = response.data.temperature;
                    this.humidity     = response.data.humidity;
                    this.co2          = response.data.co2;
                    this.sensorOnline = true;
                    this.indoorLoading = false;
                    this.lastUpdated  = new Date().toLocaleTimeString();
                    this.justUpdated  = true;
                    setTimeout(() => { this.justUpdated = false; }, 2000);
                })
                .catch(error => {
                    this.sensorOnline  = false;
                    this.indoorLoading = false;
                    console.error('Error fetching indoor sensor data:', error);
                });
        },

        // Outdoor: current weather + 5-day forecast from Open-Meteo (free, no key)
        fetchWeather() {
            axios.get(OPEN_METEO_URL)
                .then(response => {
                    const current = response.data.current;
                    this.outdoorTemp     = current.temperature_2m;
                    this.outdoorHumidity = current.relative_humidity_2m;
                    this.outdoorWind     = Math.round(current.wind_speed_10m);
                    this.weatherCode     = current.weather_code;
                    this.weatherDesc     = WMO_DESCRIPTIONS[current.weather_code] || '—';

                    const daily    = response.data.daily;
                    const dayNames = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];
                    // daily.time[0] is today — skip it, take next 5 days
                    // Parse date as local time to avoid UTC offset shifting the day name
                    this.forecast = daily.time.slice(1, 6).map((dateStr, i) => {
                        const [y, m, d] = dateStr.split('-').map(Number);
                        const localDate = new Date(y, m - 1, d);
                        return {
                            day:   dayNames[localDate.getDay()],
                            temp:  Math.round(daily.temperature_2m_max[i + 1]),
                            emoji: WMO_EMOJIS[daily.weather_code[i + 1]] || '🌡️'
                        };
                    });
                })
                .catch(error => {
                    console.error('Error fetching outdoor weather:', error);
                });
        }
    },
    mounted() {
        this.fetchData();
        this.fetchWeather();
        this.refreshInterval = setInterval(this.fetchData, 30000);      // indoor every 30 s
        this.weatherInterval = setInterval(this.fetchWeather, 600000);  // outdoor every 10 min
    },
    beforeUnmount() {
        clearInterval(this.refreshInterval);
        clearInterval(this.weatherInterval);
    }
}).mount('#app');
