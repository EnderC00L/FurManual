<template>
    <button type="button"
            class="sidebar-link w-100 text-left"
            style="border: none; background: none; cursor: pointer;"
            @click="open">
        <svg class="icon" viewBox="0 0 24 24" fill="none" stroke="currentColor">
            <path d="M15 3h4a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2h-4"></path>
            <polyline points="10 17 15 12 10 7"></polyline>
            <line x1="15" y1="12" x2="3" y2="12"></line>
        </svg>
        Войти
    </button>

    <Teleport to="body">
        <div v-if="isOpen" class="modal" style="display: flex;" @click.self="close">
            <div class="modal-content">
                <span class="close-modal" @click="close">&times;</span>
                <h2>Вход для администратора</h2>
                <form @submit.prevent="submit">
                    <div class="form-group">
                        <label for="login">Логин</label>
                        <input id="login" ref="loginInput" v-model="login" type="text" required placeholder="Введите логин" />
                    </div>
                    <div class="form-group">
                        <label for="password">Пароль</label>
                        <input id="password" v-model="password" type="password" required placeholder="Введите пароль" />
                    </div>
                    <div class="form-error">{{ errorMessage }}</div>
                    <button type="submit" class="btn-submit" :disabled="submitting">Войти</button>
                </form>
            </div>
        </div>
    </Teleport>
</template>

<script setup>
import { ref, nextTick } from 'vue';

const isOpen = ref(false);
const login = ref('');
const password = ref('');
const errorMessage = ref('');
const submitting = ref(false);
const loginInput = ref(null);

async function open() {
    isOpen.value = true;
    await nextTick();
    loginInput.value?.focus();
}

function close() {
    isOpen.value = false;
    errorMessage.value = '';
    login.value = '';
    password.value = '';
}

async function submit() {
    if (submitting.value) return;
    submitting.value = true;
    errorMessage.value = '';

    const token = document.getElementsByName('__RequestVerificationToken')[0]?.value ?? '';

    const formData = new FormData();
    formData.append('login', login.value);
    formData.append('password', password.value);

    try {
        const response = await fetch('/Login?handler=Login', {
            method: 'POST',
            body: formData,
            headers: {
                RequestVerificationToken: token,
            },
        });

        if (response.ok) {
            window.location.reload();
            return;
        }

        const text = await response.text();
        errorMessage.value = text || 'Неверный логин или пароль';
    } catch (err) {
        errorMessage.value = 'Ошибка сети';
    } finally {
        submitting.value = false;
    }
}
</script>
