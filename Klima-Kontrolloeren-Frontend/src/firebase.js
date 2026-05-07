import { initializeApp } from 'firebase/app'

const requiredEnvKeys = [
  'VITE_FIREBASE_API_KEY',
  'VITE_FIREBASE_AUTH_DOMAIN',
  'VITE_FIREBASE_PROJECT_ID',
  'VITE_FIREBASE_STORAGE_BUCKET',
  'VITE_FIREBASE_MESSAGING_SENDER_ID',
  'VITE_FIREBASE_APP_ID'
]

const missingKeys = requiredEnvKeys.filter((key) => !import.meta.env[key])

let firebaseApp = null

if (missingKeys.length > 0) {
  // Do not throw here — make firebase optional so the migrated dashboard can run
  // without Firebase configuration during local development.
  // eslint-disable-next-line no-console
  console.warn(
    `Firebase env vars missing: ${missingKeys.join(', ')} — skipping Firebase initialization.`
  )
} else {
  const firebaseConfig = {
    apiKey: import.meta.env.VITE_FIREBASE_API_KEY,
    authDomain: import.meta.env.VITE_FIREBASE_AUTH_DOMAIN,
    projectId: import.meta.env.VITE_FIREBASE_PROJECT_ID,
    storageBucket: import.meta.env.VITE_FIREBASE_STORAGE_BUCKET,
    messagingSenderId: import.meta.env.VITE_FIREBASE_MESSAGING_SENDER_ID,
    appId: import.meta.env.VITE_FIREBASE_APP_ID
  }

  firebaseApp = initializeApp(firebaseConfig)
}

export { firebaseApp }
