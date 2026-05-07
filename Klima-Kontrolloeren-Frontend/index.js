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
            ]
        };
    },
    methods: {
        fetchData() {
            axios.get('http://10.10.20.97:5000/klima-data')
                .then(response => {
                    this.temperature = response.data.temperature;
                    this.humidity = response.data.humidity;
                    this.feelsLike = response.data.feelsLike;
                    this.weatherDesc = response.data.weatherDesc || 'Overcast clouds';
                    this.co2 = response.data.co2;
                    if (response.data.forecast) {
                        this.forecast = response.data.forecast;
                    }
                })
                .catch(error => {
                    console.error('Error fetching data:', error);
                });
        }
    },
    mounted() {
        this.fetchData();
    }
}).mount('#app');
