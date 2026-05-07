<script setup>
import { computed, onMounted, ref } from 'vue'
import {
  createUserWithEmailAndPassword,
  getAuth,
  onAuthStateChanged,
  signInWithEmailAndPassword,
  signOut
} from 'firebase/auth'
import { firebaseApp } from './firebase'

const auth = getAuth(firebaseApp)
const user = ref(null)
const loading = ref(false)
const message = ref('Firebase initialized successfully.')
const email = ref('')
const password = ref('')

const firebaseDetails = computed(() => ({
  appName: firebaseApp.name,
  projectId: firebaseApp.options.projectId,
  authDomain: firebaseApp.options.authDomain
}))

onMounted(() => {
  onAuthStateChanged(auth, (currentUser) => {
    user.value = currentUser
  })
})

async function login() {
  loading.value = true
  message.value = ''

  try {
    await signInWithEmailAndPassword(auth, email.value.trim(), password.value)
    message.value = 'Signed in successfully.'
  } catch (error) {
    message.value = error.message
  } finally {
    loading.value = false
  }
}

async function register() {
  loading.value = true
  message.value = ''

  try {
    await createUserWithEmailAndPassword(auth, email.value.trim(), password.value)
    message.value = 'Account created and signed in successfully.'
  } catch (error) {
    message.value = error.message
  } finally {
    loading.value = false
  }
}

async function logout() {
  loading.value = true
  message.value = ''

  try {
    await signOut(auth)
    message.value = 'Signed out.'
  } catch (error) {
    message.value = error.message
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <main class="page">
    <section class="card">
      <p class="eyebrow">Klima Kontrolloeren</p>
      <h1>Firebase Demo</h1>
      <p class="subtitle">Your frontend is connected to Firebase through environment variables.</p>



      <div class="auth-box">
        <p v-if="user"><strong>Signed in user ID:</strong> {{ user.uid }}</p>
        <p v-if="user"><strong>Email:</strong> {{ user.email || 'No email on account' }}</p>
        <p v-else>Not signed in.</p>
      </div>

      <div class="actions">
        <input
          v-model="email"
          type="email"
          placeholder="Email"
          autocomplete="email"
          :disabled="loading || !!user"
        >
        <input
          v-model="password"
          type="password"
          placeholder="Password"
          autocomplete="current-password"
          :disabled="loading || !!user"
        >
        <button :disabled="loading || !!user || !email || !password" @click="login">
          Log in
        </button>
        <button
          :disabled="loading || !!user || !email || !password"
          class="secondary"
          @click="register"
        >
          Register
        </button>
        <button :disabled="loading || !user" class="secondary" @click="logout">Sign out</button>
      </div>

      <p class="message">{{ message }}</p>
    </section>
  </main>
</template>

<style scoped>
:global(body) {
  margin: 0;
  font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
  background: radial-gradient(circle at top, #e8f8f1, #d7ecff 45%, #f6f8fb 100%);
  color: #112233;
}

.page {
  min-height: 100vh;
  display: grid;
  place-items: center;
  padding: 1.5rem;
}

.card {
  width: min(680px, 100%);
  background: #ffffffcc;
  backdrop-filter: blur(8px);
  border: 1px solid #d0dde8;
  border-radius: 18px;
  box-shadow: 0 18px 40px #1f3b5a22;
  padding: 1.5rem;
}

.eyebrow {
  text-transform: uppercase;
  letter-spacing: 0.14em;
  font-size: 0.72rem;
  font-weight: 700;
  color: #196d56;
  margin: 0;
}

h1 {
  margin: 0.5rem 0 0.25rem;
  font-size: clamp(1.5rem, 3vw, 2.1rem);
}

.subtitle {
  margin: 0 0 1rem;
  color: #35506a;
}

.details,
.auth-box {
  border: 1px solid #d8e3ed;
  border-radius: 12px;
  padding: 0.85rem 1rem;
  background: #fff;
  margin-bottom: 0.85rem;
}

.details p,
.auth-box p {
  margin: 0.2rem 0;
}

.actions {
  display: flex;
  gap: 0.7rem;
  flex-wrap: wrap;
}

input {
  min-width: 220px;
  border: 1px solid #c6d5e3;
  border-radius: 10px;
  padding: 0.62rem 0.8rem;
  font: inherit;
  background: #fff;
}

input:disabled {
  background: #f1f5f9;
}

button {
  border: 0;
  border-radius: 10px;
  padding: 0.62rem 1rem;
  font-weight: 700;
  cursor: pointer;
  background: #1c7a63;
  color: #fff;
}

button.secondary {
  background: #32557a;
}

button:disabled {
  opacity: 0.55;
  cursor: not-allowed;
}

.message {
  margin-top: 0.9rem;
  min-height: 1.2rem;
  color: #28435d;
}

@media (max-width: 600px) {
  .card {
    padding: 1rem;
  }
}
</style>
