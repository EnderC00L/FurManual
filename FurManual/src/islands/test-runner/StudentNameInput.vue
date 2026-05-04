<template>
    <div class="input-wrapper">
        <svg class="input-icon" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"></path>
            <circle cx="12" cy="7" r="4"></circle>
        </svg>
        <input type="text"
               class="fio-input"
               :class="{ error: hasError }"
               v-model="name"
               placeholder="Введите Имя (макс. 30 симв.)"
               autocomplete="off"
               maxlength="30" />
        <div class="error-msg" :style="{ display: hasError ? 'block' : 'none' }">
            Имя не может содержать цифры!
        </div>
    </div>

    <div class="report-details" :class="{ visible: showDetails }">
        <div class="report-row"><span class="report-label">Студент</span><span class="report-value">{{ trimmedName || '-' }}</span></div>
        <div class="report-row"><span class="report-label">Тест</span><span class="report-value">{{ testTitle }}</span></div>
        <div class="report-row"><span class="report-label">Дата</span><span class="report-value">{{ testDate }}</span></div>
        <div class="report-row"><span class="report-label">Итоговый балл</span><span class="report-value">{{ score }}</span></div>
    </div>
</template>

<script setup>
import { ref, computed, watch } from 'vue';

const props = defineProps({
    testTitle: { type: String, default: '' },
    testDate: { type: String, default: '' },
    score: { type: String, default: '' },
});

function readCookie(key) {
    const parts = document.cookie.split(';');
    for (const part of parts) {
        const trimmed = part.trim();
        const eq = trimmed.indexOf('=');
        if (eq === -1) continue;
        if (trimmed.slice(0, eq) === key) {
            return decodeURIComponent(trimmed.slice(eq + 1));
        }
    }
    return null;
}

function writeCookie(key, value, days) {
    let expires = '';
    if (days) {
        const d = new Date();
        d.setTime(d.getTime() + days * 864e5);
        expires = `; expires=${d.toUTCString()}`;
    }
    document.cookie = `${key}=${encodeURIComponent(value || '')}${expires}; path=/`;
}

const name = ref(readCookie('FurManual_StudentName') || '');
const hasError = computed(() => /\d/.test(name.value));
const trimmedName = computed(() => name.value.trim());
const showDetails = computed(() => !hasError.value && trimmedName.value.length > 0);

watch(name, (value) => {
    if (!/\d/.test(value)) {
        writeCookie('FurManual_StudentName', value, 30);
    }
});
</script>
