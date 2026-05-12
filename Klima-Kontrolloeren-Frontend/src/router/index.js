import { createRouter, createWebHistory } from 'vue-router'
import { onAuthStateChanged } from 'firebase/auth'
import KlimaDashboard from '../components/KlimaDashboard.vue'
import SignInBoard from '../components/SignInBoard.vue'
import { firebaseAuth } from '../firebase'

const routes = [
  {
    path: '/',
    redirect: '/signin'
  },
  {
    path: '/signin',
    name: 'signin',
    component: SignInBoard,
    meta: { guestOnly: true }
  },
  {
    path: '/dashboard',
    name: 'dashboard',
    component: KlimaDashboard,
    meta: { requiresAuth: true }
  }
]

const router = createRouter({
  history: createWebHistory(),
  routes
})

function getCurrentUser() {
  return new Promise((resolve) => {
    const unsubscribe = onAuthStateChanged(firebaseAuth, (user) => {
      unsubscribe()
      resolve(user)
    })
  })
}

router.beforeEach(async (to) => {
  // If Firebase is not configured locally, do not block app navigation.
  if (!firebaseAuth) return true

  const user = await getCurrentUser()

  if (to.meta.requiresAuth && !user) {
    return { name: 'signin' }
  }

  if (to.meta.guestOnly && user) {
    return { name: 'dashboard' }
  }

  return true
})

export default router