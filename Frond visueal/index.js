const { createApp } = Vue;

createApp({
    data() {
        return {
            temperature: null,
            humidity: null,
            feelsLike: null,
            weatherDesc: 'Overcast clouds',
            co2: null,
            forecast: [
                { day: 'Mon', temp: 11 },
                { day: 'Tue', temp: 8 },
                { day: 'Wed', temp: 13 },
                { day: 'Thu', temp: 7 },
                { day: 'Fri', temp: 10 }
            ],
            lastUpdated: null,
            justUpdated: false,
            refreshInterval: null
        };
    },
    methods: {
        fetchData() {
            axios.get('http://10.10.20.97:5000/klima-data') //192.168.14.57 or 1.57 or home 10.10.20.97:5000
                .then(response => {
                    this.temperature = response.data.temperature;
                    this.humidity = response.data.humidity;
                    this.feelsLike = response.data.feelsLike;
                    this.weatherDesc = response.data.weatherDesc || 'Overcast clouds';
                    this.co2 = response.data.co2;
                    if (response.data.forecast) {
                        this.forecast = response.data.forecast;
                    }
                    this.lastUpdated = new Date().toLocaleTimeString();
                    this.justUpdated = true;
                    setTimeout(() => { this.justUpdated = false; }, 2000);
                })
                .catch(error => {
                    console.error('Error fetching data:', error);
                });
        }
    },
    mounted() {
        this.fetchData();
        this.refreshInterval = setInterval(this.fetchData, 30000);
    },
    beforeUnmount() {
        clearInterval(this.refreshInterval);
    }
}).mount('#app');
