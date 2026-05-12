<script setup>
import { computed, ref } from 'vue'
import { useRouter } from 'vue-router'
import {
	createUserWithEmailAndPassword,
	signInWithEmailAndPassword
} from 'firebase/auth'
import { firebaseAuth } from '../firebase'

const mode = ref('signin')
const email = ref('')
const password = ref('')
const loading = ref(false)
const errorMessage = ref('')
const successMessage = ref('')
const router = useRouter()

const isSignIn = computed(() => mode.value === 'signin')
const authUnavailable = computed(() => !firebaseAuth)

function clearMessages() {
	errorMessage.value = ''
	successMessage.value = ''
}

function switchMode(nextMode) {
	mode.value = nextMode
	clearMessages()
}

async function submitForm() {
	clearMessages()

	if (authUnavailable.value) {
		errorMessage.value = 'Firebase authentication is not configured. Please set your VITE_FIREBASE_* environment variables.'
		return
	}

	if (!email.value || !password.value) {
		errorMessage.value = 'Please fill in both email and password.'
		return
	}

	loading.value = true
	try {
		if (isSignIn.value) {
			await signInWithEmailAndPassword(firebaseAuth, email.value.trim(), password.value)
			successMessage.value = 'Signed in successfully.'
		} else {
			await createUserWithEmailAndPassword(firebaseAuth, email.value.trim(), password.value)
			successMessage.value = 'Account created and signed in.'
		}

		await router.push('/dashboard')
	} catch (error) {
		errorMessage.value = mapFirebaseError(error)
	} finally {
		loading.value = false
	}
}

function mapFirebaseError(error) {
	const code = error?.code || ''

	if (code === 'auth/invalid-email') return 'The email address is not valid.'
	if (code === 'auth/user-not-found') return 'No account exists with this email.'
	if (code === 'auth/wrong-password') return 'Incorrect password.'
	if (code === 'auth/invalid-credential') return 'Invalid credentials. Check your email and password.'
	if (code === 'auth/email-already-in-use') return 'An account already exists with this email.'
	if (code === 'auth/weak-password') return 'Password should be at least 6 characters.'
	if (code === 'auth/too-many-requests') return 'Too many attempts. Please wait and try again.'

	return error?.message || 'Sign in failed. Please try again.'
}
</script>

<template>
	<main class="signin-page">
		<section class="signin-card">
			<div class="brand-block">
				<p class="eyebrow">Klima Kontrolloeren</p>
				<h1>Sign in to your climate dashboard</h1>
				<p class="subtext">Use your Firebase account to access sensor data and controls.</p>
			</div>

			<div class="mode-toggle">
				<button
					class="mode-button"
					:class="{ active: isSignIn }"
					type="button"
					@click="switchMode('signin')"
				>
					Sign in
				</button>
				<button
					class="mode-button"
					:class="{ active: !isSignIn }"
					type="button"
					@click="switchMode('signup')"
				>
					Create account
				</button>
			</div>

			<form class="signin-form" @submit.prevent="submitForm">
				<label>
					Email
					<input
						v-model="email"
						type="email"
						autocomplete="email"
						placeholder="name@example.com"
						required
					/>
				</label>

				<label>
					Password
					<input
						v-model="password"
						type="password"
						autocomplete="current-password"
						placeholder="••••••••"
						minlength="6"
						required
					/>
				</label>

				<p v-if="errorMessage" class="feedback error">{{ errorMessage }}</p>
				<p v-if="successMessage" class="feedback success">{{ successMessage }}</p>

				<button class="submit-button" type="submit" :disabled="loading || authUnavailable">
					{{ loading ? 'Working...' : (isSignIn ? 'Sign in' : 'Create account') }}
				</button>
			</form>
		</section>
	</main>
</template>

<style scoped>
.signin-page {
	min-height: 100vh;
	display: grid;
	place-items: center;
	padding: 24px;
	background:
		radial-gradient(circle at 15% 15%, rgba(127, 216, 255, 0.35), transparent 40%),
		radial-gradient(circle at 85% 80%, rgba(76, 175, 80, 0.28), transparent 42%),
		linear-gradient(145deg, #08243f 0%, #0d3b66 45%, #114f86 100%);
}

.signin-card {
	width: min(460px, 100%);
	border-radius: 20px;
	padding: 28px;
	background: rgba(6, 24, 41, 0.84);
	border: 1px solid rgba(255, 255, 255, 0.16);
	box-shadow: 0 24px 60px rgba(0, 0, 0, 0.35);
	backdrop-filter: blur(6px);
}

.brand-block h1 {
	font-size: 1.6rem;
	margin: 6px 0 6px;
	color: #f2f8ff;
}

.eyebrow {
	font-size: 0.74rem;
	letter-spacing: 0.16em;
	text-transform: uppercase;
	color: #a9d4f0;
}

.subtext {
	color: #b8d8ec;
	font-size: 0.95rem;
	line-height: 1.45;
}

.mode-toggle {
	margin-top: 18px;
	margin-bottom: 16px;
	display: grid;
	grid-template-columns: 1fr 1fr;
	border: 1px solid rgba(255, 255, 255, 0.18);
	border-radius: 12px;
	overflow: hidden;
}

.mode-button {
	border: 0;
	background: rgba(255, 255, 255, 0.05);
	color: #c4dfef;
	padding: 10px;
	font-weight: 700;
	cursor: pointer;
}

.mode-button.active {
	background: #4caf50;
	color: #fff;
}

.signin-form {
	display: grid;
	gap: 12px;
}

label {
	display: grid;
	gap: 6px;
	color: #dbecf8;
	font-size: 0.92rem;
}

input {
	width: 100%;
	border: 1px solid rgba(255, 255, 255, 0.2);
	background: rgba(255, 255, 255, 0.08);
	color: #fff;
	border-radius: 10px;
	padding: 10px 12px;
}

input:focus {
	outline: 2px solid rgba(127, 216, 255, 0.7);
	outline-offset: 1px;
}

.feedback {
	border-radius: 8px;
	padding: 10px;
	font-size: 0.88rem;
}

.feedback.error {
	color: #ffd1d1;
	background: rgba(214, 57, 57, 0.25);
	border: 1px solid rgba(214, 57, 57, 0.45);
}

.feedback.success {
	color: #d8ffe0;
	background: rgba(76, 175, 80, 0.2);
	border: 1px solid rgba(76, 175, 80, 0.4);
}

.submit-button {
	margin-top: 6px;
	border: 0;
	border-radius: 11px;
	background: linear-gradient(120deg, #4caf50, #6bcf73);
	color: #fff;
	padding: 11px 14px;
	font-weight: 700;
	cursor: pointer;
}

.submit-button:disabled {
	opacity: 0.6;
	cursor: not-allowed;
}
</style>
