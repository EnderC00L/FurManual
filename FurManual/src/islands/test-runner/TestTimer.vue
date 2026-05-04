<template>{{ formatted }}</template>

<script setup>
import { ref, computed, onMounted, onBeforeUnmount } from 'vue';

const seconds = ref(0);
let intervalId = null;

onMounted(() => {
    intervalId = setInterval(() => {
        seconds.value += 1;
    }, 1000);
});

onBeforeUnmount(() => {
    if (intervalId) clearInterval(intervalId);
});

const formatted = computed(() => {
    const m = Math.floor(seconds.value / 60).toString().padStart(2, '0');
    const s = (seconds.value % 60).toString().padStart(2, '0');
    return `${m}:${s}`;
});
</script>
