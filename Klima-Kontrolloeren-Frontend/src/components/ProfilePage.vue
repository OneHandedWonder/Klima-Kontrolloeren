<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { updateEmail, updatePassword, reauthenticateWithCredential, EmailAuthProvider, signOut } from 'firebase/auth'
import axios from 'axios'
import { firebaseAuth } from '../firebase'

const router = useRouter()
const currentUser = firebaseAuth.currentUser

// Form state
const activeTab = ref('email') // 'email', 'password', or 'sensors'
const loading = ref(false)
const errorMessage = ref('')
const successMessage = ref('')

// Sensors management
const userSensors = ref([])
const sensorsLoading = ref(true)
const newSensorId = ref('')
const userUID = ref(null)

// Email change form
const newEmail = ref('')
const emailPassword = ref('')

// Password change form
const currentPassword = ref('')
const newPassword = ref('')
const confirmPassword = ref('')

// Validation
const emailValidation = computed(() => {
  if (!newEmail.value) return { valid: true, message: '' }
  const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/
  if (!emailRegex.test(newEmail.value)) {
    return { valid: false, message: 'Please enter a valid email address.' }
  }
  return { valid: true, message: '' }
})

const passwordValidation = computed(() => {
  if (!newPassword.value) return { valid: true, message: '' }
  if (newPassword.value.length < 6) {
    return { valid: false, message: 'Password must be at least 6 characters.' }
  }
  if (newPassword.value !== confirmPassword.value) {
    return { valid: false, message: 'Passwords do not match.' }
  }
  return { valid: true, message: '' }
})

function clearMessages() {
  errorMessage.value = ''
  successMessage.value = ''
}

function switchTab(tab) {
  activeTab.value = tab
  clearMessages()
  resetForms()
}

function resetForms() {
  newEmail.value = ''
  emailPassword.value = ''
  currentPassword.value = ''
  newPassword.value = ''
  confirmPassword.value = ''
  newSensorId.value = ''
}

// Load user's sensors
async function loadUserSensors() {
  sensorsLoading.value = true
  clearMessages()
  
  try {
    const idToken = await firebaseAuth.currentUser.getIdToken()
    const response = await axios.get(
      `https://klimakontrolloeren-backend-b8h5g9azhqdjf3gm.norwayeast-01.azurewebsites.net/api/auth/getUserUID`,
      { params: { token: idToken } }
    )
    
    userUID.value = response.data.uid
    
    // Fetch user's sensors
    const sensorResponse = await axios.get(
      `https://klimakontrolloeren-backend-b8h5g9azhqdjf3gm.norwayeast-01.azurewebsites.net/api/sensor`,
      { params: { uid: userUID.value } }
    )
    
    if (Array.isArray(sensorResponse.data) && sensorResponse.data.length > 0) {
      userSensors.value = sensorResponse.data[0].sensors || []
    }
  } catch (error) {
    console.error('Failed to load sensors:', error)
    errorMessage.value = 'Failed to load sensors'
  } finally {
    sensorsLoading.value = false
  }
}

async function addSensor() {
  clearMessages()
  
  if (!newSensorId.value.trim()) {
    errorMessage.value = 'Please enter a sensor ID'
    return
  }
  
  if (userSensors.value.includes(newSensorId.value.trim())) {
    errorMessage.value = 'This sensor ID is already added'
    return
  }
  
  loading.value = true
  
  try {
    // Add sensor to local list
    userSensors.value.push(newSensorId.value.trim())
    
    // Here you would send the update to your backend
    // For now, we'll just update locally and show success
    successMessage.value = `Sensor "${newSensorId.value.trim()}" added successfully!`
    newSensorId.value = ''
    
    // TODO: Send update to backend API endpoint
  } catch (error) {
    errorMessage.value = 'Failed to add sensor'
    userSensors.value.pop()
  } finally {
    loading.value = false
  }
}

async function removeSensor(sensorId) {
  clearMessages()
  
  if (!confirm(`Are you sure you want to remove sensor "${sensorId}"?`)) {
    return
  }
  
  loading.value = true
  
  try {
    // Remove from local list
    const index = userSensors.value.indexOf(sensorId)
    if (index > -1) {
      userSensors.value.splice(index, 1)
    }
    
    successMessage.value = `Sensor "${sensorId}" removed successfully!`
    
    // TODO: Send update to backend API endpoint
  } catch (error) {
    errorMessage.value = 'Failed to remove sensor'
  } finally {
    loading.value = false
  }
}

async function updateUserEmail() {
  clearMessages()

  if (!emailValidation.value.valid) {
    errorMessage.value = emailValidation.value.message
    return
  }

  if (!emailPassword.value) {
    errorMessage.value = 'Please enter your current password to verify your identity.'
    return
  }

  if (newEmail.value === currentUser.email) {
    errorMessage.value = 'New email must be different from your current email.'
    return
  }

  loading.value = true
  try {
    const credential = EmailAuthProvider.credential(currentUser.email, emailPassword.value)
    await reauthenticateWithCredential(currentUser, credential)
    await updateEmail(currentUser, newEmail.value)
    successMessage.value = 'Email updated successfully!'
    resetForms()
  } catch (error) {
    errorMessage.value = mapFirebaseError(error)
  } finally {
    loading.value = false
  }
}

async function updateUserPassword() {
  clearMessages()

  if (!passwordValidation.value.valid) {
    errorMessage.value = passwordValidation.value.message
    return
  }

  if (!currentPassword.value) {
    errorMessage.value = 'Please enter your current password to verify your identity.'
    return
  }

  loading.value = true
  try {
    const credential = EmailAuthProvider.credential(currentUser.email, currentPassword.value)
    await reauthenticateWithCredential(currentUser, credential)
    await updatePassword(currentUser, newPassword.value)
    successMessage.value = 'Password updated successfully!'
    resetForms()
  } catch (error) {
    errorMessage.value = mapFirebaseError(error)
  } finally {
    loading.value = false
  }
}

function mapFirebaseError(error) {
  const code = error?.code || ''

  if (code === 'auth/invalid-email') return 'The email address is not valid.'
  if (code === 'auth/email-already-in-use') return 'An account already exists with this email.'
  if (code === 'auth/wrong-password') return 'Incorrect password.'
  if (code === 'auth/invalid-credential') return 'Invalid credentials. Check your password.'
  if (code === 'auth/weak-password') return 'Password should be at least 6 characters.'
  if (code === 'auth/requires-recent-login') return 'Please sign in again before changing your email or password.'
  if (code === 'auth/too-many-requests') return 'Too many attempts. Please wait and try again.'

  return error?.message || 'An error occurred. Please try again.'
}

async function logOut() {
  try {
    await signOut(firebaseAuth)
    await router.push('/signin')
  } catch (error) {
    console.error('Logout failed:', error)
    errorMessage.value = 'Failed to sign out. Please try again.'
  }
}

onMounted(() => {
  loadUserSensors()
})
</script>

<template>
  <div class="profile-container">
    <header class="profile-header">
      <div class="header-top">
        <button class="back-button" @click="router.back()" title="Go back">← Back</button>
        <h1>Account Settings</h1>
        <div class="header-spacer"></div>
      </div>
    </header>

    <main class="profile-main">
      <div class="profile-card">
        <!-- User Info Section -->
        <section class="user-info-section">
          <div class="avatar-large">{{ currentUser?.email?.charAt(0).toUpperCase() || '?' }}</div>
          <div class="user-details">
            <h2>{{ currentUser?.email }}</h2>
            <p class="user-status">Account Status: <span class="status-active">Active</span></p>
          </div>
        </section>

        <!-- Tabs -->
        <div class="tabs">
          <button
            class="tab-button"
            :class="{ active: activeTab === 'email' }"
            @click="switchTab('email')"
          >
            Change Email
          </button>
          <button
            class="tab-button"
            :class="{ active: activeTab === 'password' }"
            @click="switchTab('password')"
          >
            Change Password
          </button>
          <button
            class="tab-button"
            :class="{ active: activeTab === 'sensors' }"
            @click="switchTab('sensors')"
          >
            Manage Sensors
          </button>
        </div>

        <!-- Messages -->
        <div v-if="errorMessage" class="message error-message">
          {{ errorMessage }}
        </div>
        <div v-if="successMessage" class="message success-message">
          {{ successMessage }}
        </div>

        <!-- Change Email Form -->
        <form v-if="activeTab === 'email'" class="settings-form" @submit.prevent="updateUserEmail">
          <div class="form-group">
            <label for="current-email">Current Email</label>
            <input
              id="current-email"
              type="email"
              :value="currentUser?.email"
              disabled
              class="form-input disabled"
            />
          </div>

          <div class="form-group">
            <label for="new-email">New Email</label>
            <input
              id="new-email"
              v-model="newEmail"
              type="email"
              placeholder="Enter your new email address"
              class="form-input"
              :class="{ 'input-error': !emailValidation.valid && newEmail }"
              required
            />
            <p v-if="!emailValidation.valid && newEmail" class="validation-message">
              {{ emailValidation.message }}
            </p>
          </div>

          <div class="form-group">
            <label for="email-password">Current Password <span class="required">*</span></label>
            <input
              id="email-password"
              v-model="emailPassword"
              type="password"
              placeholder="Enter your current password to verify"
              class="form-input"
              required
            />
            <p class="form-hint">We need your current password to verify your identity.</p>
          </div>

          <button type="submit" class="btn btn-primary" :disabled="loading || !emailValidation.valid || !newEmail || !emailPassword">
            {{ loading ? 'Updating...' : 'Update Email' }}
          </button>
        </form>

        <!-- Change Password Form -->
        <form v-if="activeTab === 'password'" class="settings-form" @submit.prevent="updateUserPassword">
          <div class="form-group">
            <label for="current-password">Current Password <span class="required">*</span></label>
            <input
              id="current-password"
              v-model="currentPassword"
              type="password"
              placeholder="Enter your current password"
              class="form-input"
              required
            />
          </div>

          <div class="form-group">
            <label for="new-password">New Password <span class="required">*</span></label>
            <input
              id="new-password"
              v-model="newPassword"
              type="password"
              placeholder="Enter a new password (min 6 characters)"
              class="form-input"
              :class="{ 'input-error': !passwordValidation.valid && newPassword }"
              required
            />
            <p v-if="newPassword.length > 0 && newPassword.length < 6" class="validation-message">
              Password must be at least 6 characters.
            </p>
          </div>

          <div class="form-group">
            <label for="confirm-password">Confirm New Password <span class="required">*</span></label>
            <input
              id="confirm-password"
              v-model="confirmPassword"
              type="password"
              placeholder="Confirm your new password"
              class="form-input"
              :class="{ 'input-error': confirmPassword && newPassword !== confirmPassword }"
              required
            />
            <p v-if="confirmPassword && newPassword !== confirmPassword" class="validation-message">
              Passwords do not match.
            </p>
          </div>

          <button type="submit" class="btn btn-primary" :disabled="loading || !passwordValidation.valid || !currentPassword || !newPassword || !confirmPassword">
            {{ loading ? 'Updating...' : 'Update Password' }}
          </button>
        </form>

        <!-- Manage Sensors Form -->
        <div v-if="activeTab === 'sensors'" class="settings-form">
          <div class="form-group">
            <label for="new-sensor">Add Sensor ID</label>
            <div class="sensor-input-group">
              <input
                id="new-sensor"
                v-model="newSensorId"
                type="text"
                placeholder="Enter sensor ID (e.g., pi-sensor-01)"
                class="form-input"
              />
              <button type="button" class="btn btn-primary" @click="addSensor" :disabled="loading || !newSensorId.trim()">
                {{ loading ? 'Adding...' : 'Add Sensor' }}
              </button>
            </div>
            <p class="form-hint">Enter your Raspberry Pi sensor ID to connect it to your account.</p>
          </div>

          <!-- Current Sensors List -->
          <div class="sensors-section">
            <h3 class="sensors-title">Your Connected Sensors</h3>
            
            <div v-if="sensorsLoading" class="sensors-loading">
              Loading sensors...
            </div>
            
            <div v-else-if="userSensors.length === 0" class="sensors-empty">
              No sensors connected yet. Add a sensor ID above to get started.
            </div>
            
            <ul v-else class="sensors-list">
              <li v-for="sensor in userSensors" :key="sensor" class="sensor-item">
                <span class="sensor-id">{{ sensor }}</span>
                <button type="button" class="btn btn-danger-small" @click="removeSensor(sensor)" :disabled="loading">
                  Remove
                </button>
              </li>
            </ul>
          </div>
        </div>
      </div>

      <!-- Logout Section -->
      <div class="logout-section">
        <button class="btn btn-logout" @click="logOut">
          Sign Out
        </button>
        <p class="logout-hint">Sign out from this device</p>
      </div>
    </main>
  </div>
</template>

<style scoped>
.profile-container {
  min-height: 100vh;
  background: linear-gradient(135deg, #0d3b66 0%, #1a5490 100%);
  padding: 40px 20px;
  color: #cfe8f7;
}

.profile-header {
  max-width: 600px;
  margin: 0 auto 30px;
}

.header-top {
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.back-button {
  background: rgba(255, 255, 255, 0.08);
  border: 1px solid rgba(255, 255, 255, 0.15);
  color: #a0c4d4;
  padding: 8px 16px;
  border-radius: 8px;
  cursor: pointer;
  font-size: 14px;
  font-weight: 500;
  transition: all 0.3s ease;
}

.back-button:hover {
  background: rgba(76, 175, 80, 0.15);
  border-color: rgba(76, 175, 80, 0.3);
  color: #a8d5b8;
}

.profile-header h1 {
  color: #cfe8f7;
  font-size: 28px;
  font-weight: 600;
  margin: 0;
  text-align: center;
  flex: 1;
}

.header-spacer {
  width: 70px;
}

.profile-main {
  max-width: 600px;
  margin: 0 auto;
}

.profile-card {
  background: linear-gradient(135deg, #0f4c81 0%, #1a5a8c 100%);
  border-radius: 15px;
  padding: 40px;
  box-shadow: 0 10px 40px rgba(0, 0, 0, 0.3);
  border: 1px solid rgba(255, 255, 255, 0.1);
  margin-bottom: 20px;
}

.user-info-section {
  display: flex;
  flex-direction: column;
  align-items: center;
  margin-bottom: 40px;
  padding-bottom: 30px;
  border-bottom: 1px solid rgba(255, 255, 255, 0.1);
}

.avatar-large {
  width: 80px;
  height: 80px;
  background: rgba(76, 175, 80, 0.2);
  border: 2px solid rgba(76, 175, 80, 0.4);
  color: #a8d5b8;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 32px;
  font-weight: bold;
  margin-bottom: 16px;
}

.user-details h2 {
  margin: 0 0 8px 0;
  font-size: 18px;
  color: #cfe8f7;
  word-break: break-all;
}

.user-status {
  margin: 0;
  font-size: 14px;
  color: #a0c4d4;
}

.status-active {
  color: #a8d5b8;
  font-weight: 600;
}

.tabs {
  display: flex;
  gap: 12px;
  margin-bottom: 30px;
  border-bottom: 1px solid rgba(255, 255, 255, 0.1);
  flex-wrap: wrap;
}

.tab-button {
  background: none;
  border: none;
  padding: 12px 16px;
  font-size: 14px;
  font-weight: 500;
  color: #7fa8bf;
  cursor: pointer;
  border-bottom: 3px solid transparent;
  transition: all 0.3s ease;
  margin-bottom: -1px;
}

.tab-button:hover {
  color: #a8d5b8;
}

.tab-button.active {
  color: #4CAF50;
  border-bottom-color: #4CAF50;
}

.message {
  padding: 12px 16px;
  border-radius: 8px;
  margin-bottom: 20px;
  font-size: 14px;
  animation: slideDown 0.3s ease;
  border-left: 4px solid;
}

@keyframes slideDown {
  from {
    opacity: 0;
    transform: translateY(-10px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

.error-message {
  background: rgba(244, 67, 54, 0.15);
  color: #ffb3ae;
  border-color: rgba(244, 67, 54, 0.4);
}

.success-message {
  background: rgba(76, 175, 80, 0.15);
  color: #a8d5b8;
  border-color: rgba(76, 175, 80, 0.4);
}

.settings-form {
  display: flex;
  flex-direction: column;
  gap: 20px;
}

.form-group {
  display: flex;
  flex-direction: column;
}

.form-group label {
  font-size: 14px;
  font-weight: 600;
  color: #cfe8f7;
  margin-bottom: 8px;
}

.required {
  color: #ffb3ae;
}

.form-input {
  padding: 10px 12px;
  border: 1px solid rgba(255, 255, 255, 0.15);
  border-radius: 8px;
  font-size: 14px;
  font-family: inherit;
  background: rgba(255, 255, 255, 0.05);
  color: #cfe8f7;
  transition: all 0.3s ease;
}

.form-input::placeholder {
  color: #7fa8bf;
}

.form-input:focus {
  outline: none;
  border-color: #4CAF50;
  background: rgba(76, 175, 80, 0.08);
  box-shadow: 0 0 0 3px rgba(76, 175, 80, 0.15);
}

.form-input.disabled {
  background-color: rgba(255, 255, 255, 0.04);
  color: #7fa8bf;
  cursor: not-allowed;
}

.form-input.input-error {
  border-color: rgba(244, 67, 54, 0.4);
  background-color: rgba(244, 67, 54, 0.08);
}

.validation-message {
  font-size: 12px;
  color: #ffb3ae;
  margin-top: 4px;
}

.form-hint {
  font-size: 12px;
  color: #a0c4d4;
  margin-top: 4px;
}

.sensor-input-group {
  display: flex;
  gap: 12px;
  align-items: flex-end;
}

.sensor-input-group .form-input {
  flex: 1;
}

.sensors-section {
  margin-top: 24px;
  padding-top: 20px;
  border-top: 1px solid rgba(255, 255, 255, 0.1);
}

.sensors-title {
  font-size: 14px;
  font-weight: 600;
  color: #cfe8f7;
  margin: 0 0 16px 0;
}

.sensors-loading,
.sensors-empty {
  padding: 16px;
  border-radius: 8px;
  background: rgba(255, 255, 255, 0.05);
  color: #a0c4d4;
  font-size: 14px;
  text-align: center;
}

.sensors-list {
  list-style: none;
  padding: 0;
  margin: 0;
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.sensor-item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 12px 16px;
  background: rgba(255, 255, 255, 0.05);
  border: 1px solid rgba(76, 175, 80, 0.2);
  border-radius: 8px;
}

.sensor-id {
  color: #cfe8f7;
  font-weight: 500;
  font-family: monospace;
}

.btn {
  padding: 12px 16px;
  border: none;
  border-radius: 8px;
  font-size: 14px;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.3s ease;
}

.btn-primary {
  background: rgba(76, 175, 80, 0.2);
  border: 1px solid rgba(76, 175, 80, 0.4);
  color: #a8d5b8;
}

.btn-primary:hover:not(:disabled) {
  background: rgba(76, 175, 80, 0.35);
  border-color: #4CAF50;
  color: #ffffff;
  box-shadow: 0 0 12px rgba(76, 175, 80, 0.3);
}

.btn-primary:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.btn-danger-small {
  padding: 6px 12px;
  font-size: 12px;
  background: rgba(244, 67, 54, 0.2);
  border: 1px solid rgba(244, 67, 54, 0.4);
  color: #ffb3ae;
}

.btn-danger-small:hover:not(:disabled) {
  background: rgba(244, 67, 54, 0.35);
  border-color: rgba(244, 67, 54, 0.6);
  color: #ffffff;
  box-shadow: 0 0 12px rgba(244, 67, 54, 0.3);
}

.btn-danger-small:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.btn-logout {
  background: rgba(244, 67, 54, 0.2);
  border: 1px solid rgba(244, 67, 54, 0.4);
  color: #ffb3ae;
  width: 100%;
}

.btn-logout:hover {
  background: rgba(244, 67, 54, 0.35);
  border-color: rgba(244, 67, 54, 0.6);
  color: #ffffff;
  box-shadow: 0 0 12px rgba(244, 67, 54, 0.3);
}

.logout-section {
  background: linear-gradient(135deg, #0f4c81 0%, #1a5a8c 100%);
  border-radius: 15px;
  padding: 24px;
  text-align: center;
  box-shadow: 0 10px 40px rgba(0, 0, 0, 0.3);
  border: 1px solid rgba(255, 255, 255, 0.1);
}

.logout-hint {
  font-size: 12px;
  color: #a0c4d4;
  margin-top: 12px;
}

@media (max-width: 600px) {
  .profile-card {
    padding: 24px 16px;
  }

  .profile-header h1 {
    font-size: 24px;
  }

  .tabs {
    gap: 8px;
  }

  .tab-button {
    padding: 10px 12px;
    font-size: 13px;
  }

  .sensor-input-group {
    flex-direction: column;
  }

  .sensor-input-group .btn {
    width: 100%;
  }
}
</style>
