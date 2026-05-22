<script setup>
import { onMounted, onBeforeUnmount, ref } from 'vue'
import { onAuthStateChanged, signOut } from 'firebase/auth'
import { RouterView, useRoute, useRouter } from 'vue-router'
import { firebaseAuth } from './firebase'

const user = ref(null)
const authLoading = ref(true)
const route = useRoute()
const router = useRouter()
let unsubscribeAuth = null
const DASHBOARD_CACHE_PREFIX = 'klima:dashboard:'

onMounted(() => {
  if (!firebaseAuth) {
    authLoading.value = false
    return
  }

  unsubscribeAuth = onAuthStateChanged(firebaseAuth, (nextUser) => {
    user.value = nextUser
    authLoading.value = false

    if (nextUser && route.meta.guestOnly) {
      router.push('/dashboard')
    }
  })
})

onBeforeUnmount(() => {
  if (unsubscribeAuth) unsubscribeAuth()
})

async function handleSignOut() {
  if (!firebaseAuth) return
  try {
    Object.keys(localStorage)
      .filter(key => key.startsWith(DASHBOARD_CACHE_PREFIX))
      .forEach(key => localStorage.removeItem(key))
  } catch (error) {
    console.warn('Failed to clear dashboard cache:', error)
  }
  await signOut(firebaseAuth)
  await router.push('/signin')
}
</script>

<template>
  <div v-if="authLoading && !route.meta.guestOnly" class="auth-loading">Checking authentication...</div>
  <template v-else>
    <RouterView />
  </template>
</template>

<style>
/* App-level styles kept minimal; main styling is loaded from assets/klima.css */
body { margin: 0 }

.auth-loading {
  min-height: 100vh;
  display: grid;
  place-items: center;
  color: #e1eef8;
}

.topbar {
  width: min(1200px, calc(100% - 40px));
  margin: 16px auto 0;
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 12px;
  color: #d8e8f4;
}

.topbar button {
  border: 1px solid rgba(255, 255, 255, 0.1);
  background: rgba(255, 255, 255, 0.06);
  color: #7fa8bf;
  padding: 8px 12px;
  border-radius: 8px;
  cursor: pointer;
  font-size: 0.85rem;
  font-weight: 400;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.3);
}
</style>
