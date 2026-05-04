<template>
    <Teleport to="body">
        <div v-if="device" class="custom-modal" style="display: flex;" @click.self="$emit('close')">
            <div class="modal-content">
                <div class="modal-header">
                    <span>Выбор порта: <span>{{ device.id }}</span></span>
                    <span style="cursor:pointer; padding: 5px;" @click="$emit('close')">✕</span>
                </div>
                <div class="modal-body">
                    <div class="port-list">
                        <div v-for="port in ports"
                             :key="port"
                             class="port-item"
                             :class="{ used: isUsed(port) }"
                             @click="onSelect(port)">
                            <span>{{ port }}</span>
                            <div class="led-indicator" :class="isUsed(port) ? 'led-busy' : 'led-free'"></div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </Teleport>
</template>

<script setup>
const props = defineProps({
    device: { type: Object, default: null },
    ports: { type: Array, default: () => [] },
});

const emit = defineEmits(['select', 'close']);

function isUsed(port) {
    return !!props.device?.usedPorts?.[port];
}

function onSelect(port) {
    if (isUsed(port)) return;
    emit('select', port);
}
</script>
