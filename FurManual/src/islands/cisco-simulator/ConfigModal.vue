<template>
    <Teleport to="body">
        <div v-if="device" class="custom-modal" style="display: flex;" @click.self="$emit('close')">
            <div class="modal-content">
                <div class="modal-header">
                    <span>Настройка <span>{{ deviceName }}</span></span>
                    <span style="cursor:pointer; padding: 5px;" @click="$emit('close')">✕</span>
                </div>
                <div class="modal-body">
                    <div class="config-form">
                        <label>IPv4 (0-255)</label>
                        <input type="text" v-model="ip" maxlength="15" placeholder="192.168.1.1" />
                        <label>Маска</label>
                        <input type="text" v-model="mask" maxlength="15" placeholder="255.255.255.0" />
                        <label>Шлюз</label>
                        <input type="text" v-model="gw" maxlength="15" />
                    </div>
                </div>
                <div class="modal-footer">
                    <button class="btn-cancel" @click="$emit('close')">Отмена</button>
                    <button type="button" class="btn-save" @click="onSave">Сохранить</button>
                </div>
            </div>
        </div>
    </Teleport>
</template>

<script setup>
import { ref, watch } from 'vue';

const props = defineProps({
    device: { type: Object, default: null },
    deviceName: { type: String, default: '' },
});

const emit = defineEmits(['save', 'close']);

const ip = ref('');
const mask = ref('');
const gw = ref('');

watch(() => props.device, (dev) => {
    if (dev) {
        ip.value = dev.ip || '';
        mask.value = dev.mask || '255.255.255.0';
        gw.value = dev.gw || '';
    }
}, { immediate: true });

function onSave() {
    emit('save', {
        ip: ip.value.trim(),
        mask: mask.value.trim(),
        gw: gw.value.trim(),
    });
}
</script>
