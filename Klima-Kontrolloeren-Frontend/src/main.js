import { createApp } from 'vue'
import App from './App.vue'
import router from './router'
import './firebase'
import './assets/klima.css'

createApp(App).use(router).mount('#app')
