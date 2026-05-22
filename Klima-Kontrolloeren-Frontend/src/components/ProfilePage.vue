<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { updateEmail, updatePassword, reauthenticateWithCredential, EmailAuthProvider } from 'firebase/auth'
import { firebaseAuth } from '../firebase'
import axios from 'axios'
const SENSOR_API = 'https://klimakontrolloeren-backend-b8h5g9azhqdjf3gm.norwayeast-01.azurewebsites.net/api/sensor'
const router = useRouter()
const currentUser = firebaseAuth.currentUser

// Form state
const activeTab = ref('email') // 'email', 'password', or 'sensors'

// ── Sensor state ──────────────────────────────────────────────
const SENSOR_TYPES = ['Temperature', 'Humidity', 'CO₂']

// Parse stored type string → array of selected types
function parseTypes(typeStr) {
  if (!typeStr) return []
  return SENSOR_TYPES.filter(t => typeStr.includes(t))
}
// Array of selected types → stored string
function joinTypes(arr) { return arr.join(', ') }

const sensors = ref([])          // [{ sensorId, name, type, location, editing, tmpName, tmpTypes, tmpLocation }]
const sensorsLoading = ref(false)
const sensorsError = ref('')
const sensorsSaving = ref(false)
const newSensorId = ref('')
const addingSensor = ref(false)
const addSensorError = ref('')
const loading = ref(false)
const errorMessage = ref('')
const successMessage = ref('')

// Email change form
const newEmail = ref('')
const emailPassword = ref('')

// Password change form
const currentPassword = ref('')
const newPassword = ref('')
const confirmPassword = ref('')

// Sensor management
const sensors = ref([])
const newSensorId = ref('')
const sensorsLoading = ref(false)
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
    // Reauthenticate user before updating email
    const credential = EmailAuthProvider.credential(currentUser.email, emailPassword.value)
    await reauthenticateWithCredential(currentUser, credential)

    // Update email
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
    // Reauthenticate user before updating password
    const credential = EmailAuthProvider.credential(currentUser.email, currentPassword.value)
    await reauthenticateWithCredential(currentUser, credential)

    // Update password
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

// ── Sensor functions ──────────────────────────────────────────
async function loadSensors() {
  if (!currentUser) return
  sensorsLoading.value = true
  sensorsError.value = ''
  try {
    const uid = currentUser.uid
    const res = await axios.get(SENSOR_API, { params: { uid } })
    const ids = res.data?.[0]?.sensors || []
    const entries = await Promise.all(ids.map(async id => {
      const r = await axios.get(`${SENSOR_API}/info`, { params: { uid, sensorId: id } }).catch(() => null)
      return { sensorId: id, name: r?.data?.name || id, type: r?.data?.type || '', location: r?.data?.location || '', editing: false, tmpName: '', tmpTypes: [], tmpLocation: '' }
    }))
    sensors.value = entries
  } catch {
    sensorsError.value = 'Could not load sensors. Please try again.'
  } finally {
    sensorsLoading.value = false
  }
}

function startEdit(sensor) {
  // Save originals — these never change, used by Cancel and on save failure
  sensor._origName     = sensor.name
  sensor._origType     = sensor.type
  sensor._origLocation = sensor.location
  // Set editable copies — v-model binds to these
  sensor.tmpName     = sensor.name
  sensor.tmpTypes    = parseTypes(sensor.type)
  sensor.tmpLocation = sensor.location
  sensor.editing = true
}

function cancelEdit(sensor) {
  // Restore originals — truly reverts all changes
  sensor.name     = sensor._origName
  sensor.type     = sensor._origType
  sensor.location = sensor._origLocation
  sensor.editing = false
}

async function saveEdit(sensor) {
  sensorsSaving.value = true
  errorMessage.value = ''
  try {
    const typeStr = joinTypes(sensor.tmpTypes)
    await axios.put(`${SENSOR_API}/info`, {
      uid: currentUser.uid,
      sensorId: sensor.sensorId,
      name: sensor.tmpName.trim() || sensor.sensorId,
      type: typeStr,
      location: sensor.tmpLocation.trim()
    })
    // Commit temps to real values only on success
    sensor.name = sensor.tmpName.trim() || sensor.sensorId
    sensor.type = typeStr
    sensor.location = sensor.tmpLocation.trim()
    sensor.editing = false
    successMessage.value = 'Sensor info saved.'
    setTimeout(() => { successMessage.value = '' }, 3000)
  } catch (e) {
    const status = e?.response?.status
    const detail = e?.response?.data?.error || e?.response?.data?.message || ''
    if (status === 404) errorMessage.value = 'Save endpoint not found on the server (HTTP 404) — the new backend code needs to be deployed.'
    else if (status === 500) errorMessage.value = `Server error — the SensorInfo database table may not exist yet. Run the CREATE TABLE SQL. ${detail}`
    else if (!status) errorMessage.value = 'Network error — check browser console (F12) for details.'
    else errorMessage.value = `Could not save (HTTP ${status}). ${detail || 'Please try again.'}`
    // Revert to originals on failure
    sensor.name     = sensor._origName
    sensor.type     = sensor._origType
    sensor.location = sensor._origLocation
  } finally { sensorsSaving.value = false }
}

async function addSensor() {
  if (!newSensorId.value.trim()) return
  addingSensor.value = true
  addSensorError.value = ''
  try {
    await axios.post(`${SENSOR_API}/add`, { uid: currentUser.uid, sensorId: newSensorId.value.trim() })
    sensors.value.push({ sensorId: newSensorId.value.trim(), name: newSensorId.value.trim(), type: '', location: '', editing: true, tmpName: newSensorId.value.trim(), tmpTypes: [], tmpLocation: '', _origName: newSensorId.value.trim(), _origType: '', _origLocation: '' })
    newSensorId.value = ''
  } catch (e) {
    const status = e?.response?.status
    if (status === 404) addSensorError.value = 'Add endpoint not found (HTTP 404) — backend needs to be deployed.'
    else addSensorError.value = e?.response?.data?.error || `Could not add sensor (HTTP ${status || 'network error'}).`
  }
  finally { addingSensor.value = false }
}

async function removeSensor(sensorId) {
  if (!confirm(`Remove sensor "${sensorId}" from your profile?`)) return
  // Remove from UI immediately
  sensors.value = sensors.value.filter(s => s.sensorId !== sensorId)
  successMessage.value = 'Sensor removed.'
  setTimeout(() => { successMessage.value = '' }, 3000)
  // Save to localStorage so dashboard also hides it (works without backend deployment)
  try {
    const key = `klima:hidden:${currentUser.uid}`
    const hidden = JSON.parse(localStorage.getItem(key) || '[]')
    if (!hidden.includes(sensorId)) { hidden.push(sensorId); localStorage.setItem(key, JSON.stringify(hidden)) }
  } catch { /* ignore */ }
  // Also call backend (works once deployed to Azure)
  axios.delete(`${SENSOR_API}/remove`, { params: { uid: currentUser.uid, sensorId } }).catch(() => {})
}

function switchToSensors() { activeTab.value = 'sensors'; clearMessages(); loadSensors() }

async function logOut() {
  try {
    await firebaseAuth.signOut()
    await router.push('/signin')
  } catch (error) {
    console.error('Logout failed:', error)
    errorMessage.value = 'Failed to sign out. Please try again.'
  }
}

// Sensor management functions
onMounted(async () => {
  if (activeTab.value === 'sensors') {
    await loadUserSensors()
  }
})

async function loadUserSensors() {
  sensorsLoading.value = true
  try {
    const token = await currentUser.getIdToken()
    const response = await axios.get(
      'https://klimakontrolloeren-backend-b8h5g9azhqdjf3gm.norwayeast-01.azurewebsites.net/api/sensor/user',
      {
        headers: { Authorization: `Bearer ${token}` }
      }
    )
    sensors.value = response.data.sensors || []
  } catch (error) {
    console.error('Failed to load sensors:', error)
    errorMessage.value = 'Failed to load sensors'
  } finally {
    sensorsLoading.value = false
  }
}

async function addSensor() {
  if (!newSensorId.value.trim()) {
    errorMessage.value = 'Please enter a sensor ID'
    return
  }

  try {
    const token = await currentUser.getIdToken()
    // TODO: Send to backend
    sensors.value.push(newSensorId.value)
    successMessage.value = 'Sensor added successfully'
    newSensorId.value = ''
  } catch (error) {
    console.error('Failed to add sensor:', error)
    errorMessage.value = 'Failed to add sensor'
  }
}

async function removeSensor(sensorId) {
  if (!confirm(`Are you sure you want to remove sensor ${sensorId}?`)) return

  try {
    const token = await currentUser.getIdToken()
    // TODO: Send to backend
    sensors.value = sensors.value.filter(s => s !== sensorId)
    successMessage.value = 'Sensor removed successfully'
  } catch (error) {
    console.error('Failed to remove sensor:', error)
    errorMessage.value = 'Failed to remove sensor'
  }
}
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
            @click="switchToSensors"
          >
            My Sensors
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

        <!-- ── My Sensors Tab ── -->
        <div v-if="activeTab === 'sensors'" class="sensors-section">
          <p class="sensors-desc">Add sensors to your profile, name them, and describe where they are placed.</p>

          <div v-if="sensorsLoading" class="sensors-loading">Loading sensors...</div>
          <div v-else-if="sensorsError" class="message error-message">{{ sensorsError }}</div>

          <template v-else>
            <!-- Sensor list -->
            <div v-if="sensors.length === 0" class="sensors-empty">No sensors yet. Add one below.</div>

            <div v-for="sensor in sensors" :key="sensor.sensorId" class="sensor-card">
              <!-- View mode -->
              <template v-if="!sensor.editing">
                <div class="sensor-card-header">
                  <div>
                    <div class="sensor-card-name">{{ sensor.name }}</div>
                    <div class="sensor-card-id">{{ sensor.sensorId }}</div>
                    <div v-if="sensor.type || sensor.location" class="sensor-card-meta">
                      <span v-if="sensor.type">🔬 {{ sensor.type }}</span>
                      <span v-if="sensor.location">📍 {{ sensor.location }}</span>
                    </div>
                  </div>
                  <div class="sensor-card-actions">
                    <button class="btn-icon btn-edit" @click="startEdit(sensor)" title="Edit">✏️ Edit</button>
                    <button class="btn-icon btn-delete" @click="removeSensor(sensor.sensorId)" title="Delete">🗑 Delete</button>
                  </div>
                </div>
              </template>

              <!-- Edit mode -->
              <template v-else>
                <div class="form-group">
                  <label>Name</label>
                  <input v-model="sensor.tmpName" type="text" class="form-input" placeholder="e.g. Living Room Sensor" maxlength="60"/>
                </div>
                <div class="form-group">
                  <label>Type</label>
                  <div class="sensor-type-checks">
                    <label v-for="t in SENSOR_TYPES" :key="t" class="type-check-label">
                      <input type="checkbox" :value="t" v-model="sensor.tmpTypes" class="type-check-input"/>
                      {{ t }}
                    </label>
                  </div>
                </div>
                <div class="form-group">
                  <label>Location</label>
                  <input v-model="sensor.tmpLocation" type="text" class="form-input" placeholder="e.g. Living Room, 1st floor" maxlength="100"/>
                </div>
                <div class="sensor-edit-btns">
                  <button class="btn btn-primary" :disabled="sensorsSaving" @click="saveEdit(sensor)">
                    {{ sensorsSaving ? 'Saving...' : 'Save' }}
                  </button>
                  <button class="btn btn-secondary" @click="cancelEdit(sensor)">Cancel</button>
                </div>
              </template>
            </div>

            <!-- Add new sensor -->
            <div class="sensor-add-row">
              <input v-model="newSensorId" type="text" class="form-input" placeholder="Sensor ID (e.g. pi-sensor-02)" maxlength="128"/>
              <button class="btn btn-add-sensor" :disabled="addingSensor || !newSensorId.trim()" @click="addSensor">
                {{ addingSensor ? 'Adding...' : '+ Add Sensor' }}
              </button>
            </div>
            <p v-if="addSensorError" class="add-sensor-error">{{ addSensorError }}</p>
          </template>
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

/* Sensors Section */
.sensors-section {
  display: flex;
  flex-direction: column;
  gap: 20px;
}

.loading-message {
  text-align: center;
  color: #a0c4d4;
  font-size: 14px;
  padding: 20px;
}

.no-sensors-message {
  text-align: center;
  color: #a0c4d4;
  font-size: 14px;
  padding: 20px;
  background: rgba(160, 196, 212, 0.05);
  border-radius: 8px;
  border: 1px solid rgba(160, 196, 212, 0.1);
}

.sensors-list {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.sensor-item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 12px 16px;
  background: rgba(76, 175, 80, 0.08);
  border: 1px solid rgba(76, 175, 80, 0.2);
  border-radius: 8px;
}

.sensor-name {
  color: #cfe8f7;
  font-size: 14px;
  font-weight: 500;
}

.btn-remove {
  background: rgba(244, 67, 54, 0.15);
  border: 1px solid rgba(244, 67, 54, 0.3);
  color: #ffb3ae;
  padding: 6px 10px;
  border-radius: 6px;
  cursor: pointer;
  font-size: 16px;
  transition: all 0.3s ease;
}

.btn-remove:hover {
  background: rgba(244, 67, 54, 0.3);
  border-color: rgba(244, 67, 54, 0.5);
  color: #ffffff;
}

.add-sensor-form {
  display: flex;
  gap: 10px;
}

.add-sensor-form .form-input {
  flex: 1;
}

.btn-add-sensor {
  background: rgba(76, 175, 80, 0.2);
  border: 1px solid rgba(76, 175, 80, 0.4);
  color: #a8d5b8;
  padding: 10px 16px;
  border-radius: 8px;
  cursor: pointer;
  font-weight: 600;
  transition: all 0.3s ease;
  white-space: nowrap;
}

.btn-add-sensor:hover:not(:disabled) {
  background: rgba(76, 175, 80, 0.35);
  border-color: #4CAF50;
  color: #ffffff;
  box-shadow: 0 0 12px rgba(76, 175, 80, 0.3);
}

.btn-add-sensor:disabled {
  opacity: 0.5;
  cursor: not-allowed;
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

  .add-sensor-form {
    flex-direction: column;
  }
}

/* ── Sensor tab styles ── */
.sensors-desc { font-size: 0.9rem; color: #8b949e; margin-bottom: 1.25rem; }
.sensors-loading, .sensors-empty { color: #8b949e; font-size: 0.9rem; padding: 0.75rem 0; }
.sensor-card {
  background: rgba(255,255,255,0.04);
  border: 1px solid rgba(255,255,255,0.08);
  border-radius: 10px;
  padding: 1rem 1.1rem;
  margin-bottom: 0.85rem;
}
.sensor-card-header { display: flex; justify-content: space-between; align-items: flex-start; gap: 1rem; }
.sensor-card-name { font-size: 1rem; font-weight: 600; color: #c9d1d9; }
.sensor-card-id { font-size: 0.72rem; color: #8b949e; font-family: monospace; margin-top: 2px; }
.sensor-card-meta { display: flex; gap: 12px; margin-top: 6px; font-size: 0.82rem; color: #7fa8bf; }
.sensor-card-actions { display: flex; gap: 8px; flex-shrink: 0; }
.btn-icon {
  padding: 5px 10px;
  border-radius: 6px;
  border: 1px solid;
  cursor: pointer;
  font-size: 0.78rem;
  transition: all 0.2s;
}
.btn-edit { background: rgba(88,166,255,0.1); border-color: rgba(88,166,255,0.3); color: #58a6ff; }
.btn-edit:hover { background: rgba(88,166,255,0.2); }
.btn-delete { background: rgba(244,67,54,0.1); border-color: rgba(244,67,54,0.3); color: #f44336; }
.btn-delete:hover { background: rgba(244,67,54,0.2); }
.sensor-type-checks { display: flex; gap: 1.25rem; flex-wrap: wrap; padding: 8px 0; }
.type-check-label { display: flex; align-items: center; gap: 6px; color: #c9d1d9; font-size: 0.9rem; cursor: pointer; }
.type-check-input { width: 16px; height: 16px; accent-color: #4CAF50; cursor: pointer; }
.sensor-edit-btns { display: flex; gap: 0.75rem; margin-top: 0.5rem; }
.sensor-add-row { display: flex; gap: 0.75rem; margin-top: 1rem; }
.sensor-add-row .form-input { flex: 1; margin-bottom: 0; }
.btn-add-sensor {
  padding: 0.75rem 1.25rem;
  background: rgba(76, 175, 80, 0.15);
  border: 1px solid rgba(76, 175, 80, 0.5);
  color: #4CAF50;
  border-radius: 6px;
  cursor: pointer;
  font-size: 0.95rem;
  font-weight: 600;
  white-space: nowrap;
  transition: all 0.2s ease;
}
.btn-add-sensor:hover:not(:disabled) {
  background: rgba(76, 175, 80, 0.25);
  border-color: #4CAF50;
  box-shadow: 0 0 10px rgba(76, 175, 80, 0.2);
}
.btn-add-sensor:disabled { opacity: 0.4; cursor: not-allowed; }
.add-sensor-error { color: #f44336; font-size: 0.82rem; margin-top: 6px; }
</style>
