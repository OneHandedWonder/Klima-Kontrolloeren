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

onMounted(() => {
  if (!firebaseAuth) {
    authLoading.value = false
    return
  }

  unsubscribeAuth = onAuthStateChanged(firebaseAuth, (nextUser) => {
    user.value = nextUser
    authLoading.value = false
  })
})

onBeforeUnmount(() => {
  if (unsubscribeAuth) unsubscribeAuth()
})

async function handleSignOut() {
  if (!firebaseAuth) return
  await signOut(firebaseAuth)
  await router.push('/signin')
}
</script>

<template>
  <div v-if="authLoading" class="auth-loading">Checking authentication...</div>
  <template v-else>
    <header v-if="user && route.name === 'dashboard'" class="topbar">
      <p>{{ user.email }}</p>
      <button type="button" @click="handleSignOut">Sign out</button>
    </header>
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
  border: 1px solid rgba(255, 255, 255, 0.25);
  background: rgba(255, 255, 255, 0.08);
  color: #fff;
  padding: 8px 12px;
  border-radius: 10px;
  cursor: pointer;
}
</style>
