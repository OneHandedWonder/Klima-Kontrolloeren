import { initializeApp } from 'firebase/app'
import { getAuth } from 'firebase/auth'

const firebaseConfig = {
  apiKey: 'AIzaSyD8UKI2QndJsNJsl_dx1YyykuN5ZYxDpJo',
  authDomain: 'testprojectmichael-aeac9.firebaseapp.com',
  projectId: 'testprojectmichael-aeac9',
  storageBucket: 'testprojectmichael-aeac9.firebasestorage.app',
  messagingSenderId: '111678406050',
  appId: '1:111678406050:web:b9d747504885d65c7586fb'
}

const firebaseApp = initializeApp(firebaseConfig)
const firebaseAuth = getAuth(firebaseApp)

export { firebaseApp, firebaseAuth }
