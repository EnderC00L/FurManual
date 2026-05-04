<template>
    <details class="calc-disclosure">
        <summary class="calc-summary">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" width="24" height="24">
                <rect x="2" y="2" width="20" height="20" rx="2" ry="2"></rect>
                <line x1="16" y1="2" x2="16" y2="6"></line>
                <line x1="8" y1="2" x2="8" y2="6"></line>
                <line x1="2" y1="10" x2="22" y2="10"></line>
                <path d="M7 15h.01M12 15h.01M17 15h.01"></path>
            </svg>
            IP / Subnet Калькулятор
        </summary>

        <div class="calc-panel-body">
            <div class="form-grid">
                <div class="form-group">
                    <label for="ipAddress">IP-адрес</label>
                    <input id="ipAddress" v-model="ipInput" type="text" placeholder="Например: 192.168.1.10" class="search-input" />
                </div>
                <div class="form-group">
                    <label for="subnetMask">Маска подсети (CIDR или формат 255...)</label>
                    <input id="subnetMask" v-model="maskInput" type="text" placeholder="Например: 24 или 255.255.255.0" class="search-input" />
                </div>
            </div>

            <label class="checkbox-group">
                <input type="checkbox" v-model="showBinary" />
                <span>Показать в двоичном формате (Сеть - <span style="color:var(--accent-primary);font-weight:bold;">Синий</span>, Хост - <span style="color:#94a3b8;font-weight:bold;">Серый</span>)</span>
            </label>

            <button type="button" class="btn-submit" @click="calculate">Рассчитать</button>

            <div class="calc-error" v-if="error" style="display: block">{{ error }}</div>

            <div class="calc-results" :class="{ active: !!results }">
                <div class="results-grid">
                    <div class="result-item">
                        <span class="result-label">Адрес сети (Network)</span>
                        <span class="result-value">{{ results?.network ?? '-' }}</span>
                        <div class="binary-val" :style="binaryStyle" v-html="results?.binNetwork ?? ''"></div>
                    </div>
                    <div class="result-item">
                        <span class="result-label">Широковещательный (Broadcast)</span>
                        <span class="result-value">{{ results?.broadcast ?? '-' }}</span>
                        <div class="binary-val" :style="binaryStyle" v-html="results?.binBroadcast ?? ''"></div>
                    </div>
                    <div class="result-item">
                        <span class="result-label">Первый хост (Min IP)</span>
                        <span class="result-value">{{ results?.first ?? '-' }}</span>
                        <div class="binary-val" :style="binaryStyle" v-html="results?.binFirst ?? ''"></div>
                    </div>
                    <div class="result-item">
                        <span class="result-label">Последний хост (Max IP)</span>
                        <span class="result-value">{{ results?.last ?? '-' }}</span>
                        <div class="binary-val" :style="binaryStyle" v-html="results?.binLast ?? ''"></div>
                    </div>
                    <div class="result-item">
                        <span class="result-label">Маска подсети</span>
                        <span class="result-value">{{ results?.mask ?? '-' }}</span>
                        <div class="binary-val" :style="binaryStyle" v-html="results?.binMask ?? ''"></div>
                    </div>
                    <div class="result-item">
                        <span class="result-label">Всего хостов (Usable)</span>
                        <span class="result-value">{{ results?.hosts ?? '-' }}</span>
                    </div>
                </div>
            </div>
        </div>
    </details>
</template>

<script setup>
import { ref, computed } from 'vue';

const ipInput = ref('');
const maskInput = ref('');
const showBinary = ref(false);
const error = ref('');
const results = ref(null);

const binaryStyle = computed(() => ({ display: showBinary.value ? 'block' : 'none' }));

const IP_REGEX = /^(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$/;

function ipToLong(ip) {
    return ip.split('.').reduce((acc, octet) => (acc << 8) + parseInt(octet, 10), 0) >>> 0;
}

function longToIp(long) {
    return [
        (long >>> 24) & 255,
        (long >>> 16) & 255,
        (long >>> 8) & 255,
        long & 255,
    ].join('.');
}

function getMaskLength(maskStr) {
    if (maskStr.includes('.')) {
        const maskLong = ipToLong(maskStr);
        let len = 0;
        for (let i = 31; i >= 0; i--) {
            if ((maskLong & (1 << i)) !== 0) len++;
            else break;
        }
        return len;
    }
    return parseInt(maskStr.replace('/', ''), 10);
}

function toBinaryHTML(ipLong, maskLen) {
    const binStr = (ipLong >>> 0).toString(2).padStart(32, '0');
    let html = '';
    for (let i = 0; i < 32; i++) {
        if (i > 0 && i % 8 === 0) html += '.';
        const bitClass = i < maskLen ? 'bin-net' : 'bin-host';
        html += `<span class="${bitClass}">${binStr[i]}</span>`;
    }
    return html;
}

function calculate() {
    error.value = '';
    results.value = null;

    const ip = ipInput.value.trim();
    const mask = maskInput.value.trim();

    if (!IP_REGEX.test(ip)) {
        error.value = 'Введите корректный IP-адрес (например: 192.168.0.1).';
        return;
    }

    const maskLen = getMaskLength(mask);
    if (Number.isNaN(maskLen) || maskLen < 0 || maskLen > 32) {
        error.value = 'Неверный формат маски подсети.';
        return;
    }

    const ipLong = ipToLong(ip);
    const maskLong = maskLen === 0 ? 0 : (~0 << (32 - maskLen)) >>> 0;
    const networkLong = (ipLong & maskLong) >>> 0;
    const broadcastLong = (networkLong | ~maskLong) >>> 0;

    let firstHost = networkLong + 1;
    let lastHost = broadcastLong - 1;
    let totalHosts = lastHost - firstHost + 1;

    if (maskLen === 32) {
        firstHost = networkLong;
        lastHost = networkLong;
        totalHosts = 1;
    } else if (maskLen === 31) {
        firstHost = networkLong;
        lastHost = broadcastLong;
        totalHosts = 2;
    }

    results.value = {
        network: `${longToIp(networkLong)}/${maskLen}`,
        broadcast: longToIp(broadcastLong),
        first: longToIp(firstHost),
        last: longToIp(lastHost),
        mask: longToIp(maskLong),
        hosts: totalHosts > 0 ? totalHosts.toLocaleString() : '0',
        binNetwork: toBinaryHTML(networkLong, maskLen),
        binBroadcast: toBinaryHTML(broadcastLong, maskLen),
        binFirst: toBinaryHTML(firstHost, maskLen),
        binLast: toBinaryHTML(lastHost, maskLen),
        binMask: toBinaryHTML(maskLong, maskLen),
    };
}
</script>
