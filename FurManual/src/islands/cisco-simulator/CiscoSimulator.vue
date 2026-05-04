<template>
    <div class="cisco-container">
        <!-- Mobile task list -->
        <details class="mobile-tasks-details">
            <summary class="mobile-summary">Учебный план (Выбрать задание)</summary>
            <div class="mobile-task-list">
                <template v-for="(group, chapter) in groupedTasks" :key="chapter">
                    <div class="chapter-title">{{ chapter }}</div>
                    <div v-for="task in group"
                         :key="task.Id"
                         class="task-item"
                         :class="{ active: task.Id === currentTaskId }"
                         @click="selectTask(task.Id)">
                        <div class="status-dot" :class="{ completed: completedTaskIds.has(task.Id) }"></div>
                        <div style="font-size: 0.9rem;">{{ task.Title }}</div>
                    </div>
                </template>
            </div>
        </details>

        <!-- Sidebar tasks -->
        <aside class="panel tasks-panel">
            <div style="padding: 1rem; border-bottom: 1px solid #eee; font-weight: bold;">Учебный план</div>
            <div style="overflow-y: auto; flex-grow: 1;">
                <template v-for="(group, chapter) in groupedTasks" :key="chapter">
                    <div class="chapter-title">{{ chapter }}</div>
                    <div v-for="task in group"
                         :key="task.Id"
                         class="task-item"
                         :class="{ active: task.Id === currentTaskId }"
                         @click="selectTask(task.Id)">
                        <div class="status-dot" :class="{ completed: completedTaskIds.has(task.Id) }"></div>
                        <div>
                            <div style="font-weight: 600; font-size: 0.9rem;">{{ task.Title }}</div>
                            <div style="font-size: 0.75rem; color: #94a3b8;">Сложность: {{ stars(task.Difficulty) }}</div>
                        </div>
                    </div>
                </template>
            </div>
        </aside>

        <!-- Toolbar -->
        <div class="panel toolbar-panel">
            <div class="tool-group">
                <div v-for="type in deviceTypes"
                     :key="type"
                     class="device-tool"
                     :class="{ pressing: pressingTool === type }"
                     :data-type="type"
                     draggable="true"
                     @dragstart="onToolDragStart($event, type)"
                     @touchstart="onToolTouchStart($event, type)"
                     @touchmove="onToolTouchMove"
                     @touchend="onToolTouchEnd">
                    <svg :style="{ width: '28px', height: '28px', stroke: deviceConfig[type].color, fill: 'none', strokeWidth: 2 }">
                        <use :href="deviceConfig[type].icon" />
                    </svg>
                    <span class="tool-label">{{ deviceConfig[type].name }}</span>
                </div>
            </div>
            <div class="tool-group">
                <div class="action-tool" :class="{ active: currentMode === 'cable_straight' }" @click="toggleMode('cable_straight')">
                    <svg style="width:24px;height:24px;stroke:black;fill:none;"><use href="#icon-cable-straight" /></svg>
                    <span class="tool-label">Прямой</span>
                </div>
                <div class="action-tool" :class="{ active: currentMode === 'cable_cross' }" @click="toggleMode('cable_cross')">
                    <svg style="width:24px;height:24px;stroke:black;fill:none;"><use href="#icon-cable-cross" /></svg>
                    <span class="tool-label">Кросс</span>
                </div>
            </div>
            <div class="tool-group">
                <div class="action-tool" :class="{ active: currentMode === 'ping' }" @click="toggleMode('ping')">
                    <svg style="width:24px;height:24px;stroke:#8b5cf6;fill:none;stroke-width:2;"><use href="#icon-ping" /></svg>
                    <span class="tool-label">Ping</span>
                </div>
                <div class="action-tool delete-mode" :class="{ active: currentMode === 'delete' }" @click="toggleMode('delete')">
                    <svg style="width:24px;height:24px;stroke:#ef4444;fill:none;stroke-width:2;"><use href="#icon-trash" /></svg>
                    <span class="tool-label">Удалить</span>
                </div>
            </div>
        </div>

        <!-- Workspace -->
        <div ref="workspaceEl"
             class="panel workspace-panel"
             :style="{ cursor: currentMode === 'delete' ? 'crosshair' : 'default' }"
             @drop.prevent="onWorkspaceDrop"
             @dragover.prevent
             @click="onWorkspaceBgClick">
            <svg class="connections-layer">
                <line v-for="c in connections"
                      :key="c.id"
                      class="cable-line"
                      :class="c.type === 'cable_cross' ? 'cable-cross' : 'cable-straight'"
                      :x1="(devices[c.from]?.x ?? 0) + 35"
                      :y1="(devices[c.from]?.y ?? 0) + 35"
                      :x2="(devices[c.to]?.x ?? 0) + 35"
                      :y2="(devices[c.to]?.y ?? 0) + 35"
                      @click.stop="onCableClick(c.id)" />
            </svg>

            <div v-for="dev in deviceList"
                 :key="dev.id"
                 :id="dev.id"
                 class="network-node"
                 :style="{ left: dev.x + 'px', top: dev.y + 'px' }"
                 @mousedown="onDeviceMouseDown(dev.id, $event)"
                 @touchstart="onDeviceTouchStart(dev.id, $event)"
                 @dblclick="openConfig(dev.id)">
                <svg :style="{ width: '50px', height: '50px', stroke: deviceConfig[dev.type].color, fill: 'white', strokeWidth: 2 }">
                    <use :href="deviceConfig[dev.type].icon" />
                </svg>
                <span class="node-label">{{ deviceConfig[dev.type].name }} {{ dev.counter }}</span>
                <div v-if="dev.ip" class="node-ip" style="display: block;">{{ dev.ip }}</div>
                <div v-if="ledStatus[dev.id]"
                     class="link-status"
                     :class="{ up: ledStatus[dev.id] === 'up' }"
                     style="bottom:-5px;right:-5px;display:block;"></div>
            </div>

            <div v-show="packet.visible" class="packet" :style="{ left: packet.x + 'px', top: packet.y + 'px' }"></div>

            <div v-if="notification"
                 :key="notificationKey"
                 class="mascot-overlay show"
                 @click="replayAnimation">
                <div class="mascot-bubble" :style="{ '--bubble-color': notification.color }">{{ notification.text }}</div>
                <img :src="mascotImgSrc" class="mascot-img-popup" />
            </div>
        </div>

        <!-- Mascot panel -->
        <div class="panel mascot-panel">
            <div class="mascot-dialog">
                <div>
                    <h4 style="margin:0 0 0.5rem 0;color:var(--accent-primary);">Задание:</h4>
                    <p style="color:#334155;line-height:1.5;white-space: pre-wrap; font-size: 0.95rem; margin:0;">
                        {{ currentTask?.Description ?? 'Выберите задание...' }}
                    </p>
                </div>
                <button class="btn-check" @click="checkSolution">ПРОВЕРИТЬ</button>
            </div>
        </div>
    </div>

    <ConfigModal :device="editingDevice"
                 :device-name="editingDeviceName"
                 @save="saveConfig"
                 @close="closeConfig" />

    <PortModal :device="portModalDevice"
               :ports="portModalPorts"
               @select="onPortSelect"
               @close="closePortModal" />
</template>

<script setup>
import { ref, reactive, computed, onMounted, onBeforeUnmount } from 'vue';
import ConfigModal from './ConfigModal.vue';
import PortModal from './PortModal.vue';
import {
    DEVICE_CONFIG,
    IP_REGEX,
    ipToLong,
    getMaskLength,
    validateCable,
    findPathBFS,
    readCookie,
    writeCookie,
} from './network.js';

const props = defineProps({
    tasks: { type: Array, default: () => [] },
});

const mascotImgSrc = '/Res/Helpers/Hmm.png';

// --- State ---
const deviceTypes = ['pc', 'switch', 'router'];
const deviceConfig = DEVICE_CONFIG;

const devices = reactive({});
const connections = ref([]);
const deviceCounter = ref(0);

const currentMode = ref(null);
const pendingAction = ref(null);
const hasSuccessfulPing = ref(false);

const currentTaskId = ref(0);
const currentCriteria = ref(null);
const completedTaskIds = ref(new Set());

const editingDeviceId = ref(null);
const portModalDeviceId = ref(null);

const notification = ref(null);
const notificationKey = ref(0);
let notifTimeout = null;

const packet = reactive({ visible: false, x: 0, y: 0 });

const workspaceEl = ref(null);

// --- Drag state (non-reactive) ---
let dragItem = null;
let dragOffset = { x: 0, y: 0 };
let touchStartTime = 0;
let touchStartPos = { x: 0, y: 0 };
const pressingTool = ref(null);
let longPressTimer = null;
let touchGhost = null;
let touchType = null;

// --- Computed ---
const deviceList = computed(() => Object.values(devices));

const groupedTasks = computed(() => {
    const groups = {};
    for (const t of props.tasks) {
        const ch = t.Chapter || 'Общее';
        if (!groups[ch]) groups[ch] = [];
        groups[ch].push(t);
    }
    return groups;
});

const currentTask = computed(() => props.tasks.find(t => t.Id === currentTaskId.value));

const editingDevice = computed(() => editingDeviceId.value ? devices[editingDeviceId.value] : null);
const editingDeviceName = computed(() => {
    const dev = editingDevice.value;
    return dev ? `${DEVICE_CONFIG[dev.type].name} ${dev.counter}` : '';
});

const portModalDevice = computed(() => portModalDeviceId.value ? devices[portModalDeviceId.value] : null);
const portModalPorts = computed(() => {
    const dev = portModalDevice.value;
    return dev ? DEVICE_CONFIG[dev.type].ports : [];
});

const ledStatus = computed(() => {
    const result = {};
    for (const c of connections.value) {
        const d1 = devices[c.from];
        const d2 = devices[c.to];
        if (!d1 || !d2) continue;
        let isUp = c.status;
        if (d1.type === 'router' && !d1.ip) isUp = false;
        if (d2.type === 'router' && !d2.ip) isUp = false;
        result[c.from] = isUp ? 'up' : 'down';
        result[c.to] = isUp ? 'up' : 'down';
    }
    return result;
});

// --- Tasks ---
function selectTask(id) {
    const task = props.tasks.find(t => t.Id === id);
    if (!task) return;
    currentTaskId.value = id;
    try { currentCriteria.value = JSON.parse(task.ValidationCriteria || '{}'); }
    catch { currentCriteria.value = {}; }

    // Reset workspace
    for (const k of Object.keys(devices)) delete devices[k];
    connections.value = [];
    deviceCounter.value = 0;
    currentMode.value = null;
    pendingAction.value = null;
    hasSuccessfulPing.value = false;
    notification.value = null;
    editingDeviceId.value = null;
    portModalDeviceId.value = null;
    packet.visible = false;
}

function stars(n) { return '⭐'.repeat(n || 0); }

// --- Mode ---
function toggleMode(m) {
    currentMode.value = currentMode.value === m ? null : m;
    pendingAction.value = null;
}

// --- Devices ---
function createDevice(type, x, y) {
    deviceCounter.value++;
    const counter = deviceCounter.value;
    const id = `dev-${counter}`;
    devices[id] = {
        id,
        type,
        x,
        y,
        counter,
        usedPorts: {},
        ip: '',
        mask: '',
        gw: '',
    };
}

function deleteDevice(id) {
    const remaining = [];
    for (const c of connections.value) {
        if (c.from === id || c.to === id) {
            const otherId = c.from === id ? c.to : c.from;
            const otherPort = c.from === id ? c.toPort : c.fromPort;
            if (devices[otherId]) delete devices[otherId].usedPorts[otherPort];
        } else {
            remaining.push(c);
        }
    }
    connections.value = remaining;
    delete devices[id];

    if (pendingAction.value && pendingAction.value.devId === id) {
        pendingAction.value = null;
    }
}

// --- Tool palette: HTML5 mouse drag ---
function onToolDragStart(e, type) {
    e.dataTransfer.setData('type', type);
}

function onWorkspaceDrop(e) {
    const type = e.dataTransfer.getData('type');
    if (!type) return;
    const rect = workspaceEl.value.getBoundingClientRect();
    createDevice(type, e.clientX - rect.left - 35, e.clientY - rect.top - 35);
}

// --- Tool palette: touch long-press ---
function onToolTouchStart(e, type) {
    if (e.touches.length > 1) return;
    pressingTool.value = type;
    longPressTimer = setTimeout(() => {
        pressingTool.value = null;
        startTouchDrag(e.touches[0], type);
    }, 300);
}

function onToolTouchMove() {
    clearTimeout(longPressTimer);
    pressingTool.value = null;
}

function onToolTouchEnd() {
    clearTimeout(longPressTimer);
    pressingTool.value = null;
}

function startTouchDrag(touch, type) {
    touchType = type;
    if (window.navigator.vibrate) window.navigator.vibrate(50);
    touchGhost = document.createElement('div');
    touchGhost.style.cssText = 'width:50px;height:50px;background:rgba(102,126,234,0.5);border:2px solid blue;border-radius:50%;position:fixed;z-index:9999;pointer-events:none;';
    document.body.appendChild(touchGhost);
    moveGhost(touch);
    document.addEventListener('touchmove', onTouchDragMove, { passive: false });
    document.addEventListener('touchend', onTouchDragEnd);
}

function onTouchDragMove(e) {
    e.preventDefault();
    moveGhost(e.touches[0]);
}

function moveGhost(touch) {
    if (!touchGhost) return;
    touchGhost.style.left = (touch.clientX - 25) + 'px';
    touchGhost.style.top = (touch.clientY - 25) + 'px';
}

function onTouchDragEnd(e) {
    document.removeEventListener('touchmove', onTouchDragMove);
    document.removeEventListener('touchend', onTouchDragEnd);
    const touch = e.changedTouches[0];
    const elementUnder = document.elementFromPoint(touch.clientX, touch.clientY);
    const ws = workspaceEl.value;
    if (ws && (ws.contains(elementUnder) || elementUnder === ws)) {
        const rect = ws.getBoundingClientRect();
        createDevice(touchType, touch.clientX - rect.left - 25, touch.clientY - rect.top - 25);
    }
    if (touchGhost) { touchGhost.remove(); touchGhost = null; }
}

// --- Device clicks: mode-aware ---
function onDeviceMouseDown(devId, e) {
    e.stopPropagation();
    if (currentMode.value === 'delete') { deleteDevice(devId); return; }
    if (currentMode.value === 'ping') { handlePing(devId); return; }
    if (currentMode.value === 'cable_straight' || currentMode.value === 'cable_cross') {
        showPortMenu(devId);
        return;
    }
    // Start move
    const rect = e.currentTarget.getBoundingClientRect();
    dragOffset = { x: e.clientX - rect.left, y: e.clientY - rect.top };
    dragItem = devId;
    document.addEventListener('mousemove', onMouseMove);
    document.addEventListener('mouseup', onMouseUp);
}

function onDeviceTouchStart(devId, e) {
    e.stopPropagation();
    if (e.cancelable) e.preventDefault();
    if (e.touches.length > 1) return;
    const touch = e.touches[0];
    touchStartTime = Date.now();
    touchStartPos = { x: touch.clientX, y: touch.clientY };

    const rect = e.currentTarget.getBoundingClientRect();
    dragOffset = { x: touch.clientX - rect.left, y: touch.clientY - rect.top };
    dragItem = devId;
    document.addEventListener('touchmove', onTouchMove, { passive: false });
    document.addEventListener('touchend', onTouchEnd);
}

function onMouseMove(e) {
    moveDeviceLogic(e.clientX, e.clientY);
}

function onTouchMove(e) {
    e.preventDefault();
    moveDeviceLogic(e.touches[0].clientX, e.touches[0].clientY);
}

function moveDeviceLogic(clientX, clientY) {
    if (!dragItem) return;
    const ws = workspaceEl.value;
    const rect = ws.getBoundingClientRect();
    const x = Math.max(0, Math.min(clientX - rect.left - dragOffset.x, ws.offsetWidth - 70));
    const y = Math.max(0, Math.min(clientY - rect.top - dragOffset.y, ws.offsetHeight - 70));
    if (devices[dragItem]) {
        devices[dragItem].x = x;
        devices[dragItem].y = y;
    }
}

function onMouseUp() {
    dragItem = null;
    document.removeEventListener('mousemove', onMouseMove);
    document.removeEventListener('mouseup', onMouseUp);
}

function onTouchEnd(e) {
    if (e.cancelable) e.preventDefault();
    const id = dragItem;
    dragItem = null;
    document.removeEventListener('touchmove', onTouchMove);
    document.removeEventListener('touchend', onTouchEnd);

    const touch = e.changedTouches[0];
    const dist = Math.hypot(touch.clientX - touchStartPos.x, touch.clientY - touchStartPos.y);
    const time = Date.now() - touchStartTime;

    if (time < 400 && dist < 20 && id) {
        if (window.navigator.vibrate) window.navigator.vibrate(30);
        if (currentMode.value === 'delete') { deleteDevice(id); return; }
        if (currentMode.value === 'ping') { handlePing(id); return; }
        if (currentMode.value === 'cable_straight' || currentMode.value === 'cable_cross') {
            showPortMenu(id);
            return;
        }
        if (!currentMode.value) openConfig(id);
    }
}

// --- Cable connections ---
function showPortMenu(devId) {
    portModalDeviceId.value = devId;
}

function closePortModal() {
    portModalDeviceId.value = null;
}

function onPortSelect(port) {
    const devId = portModalDeviceId.value;
    closePortModal();
    if (!devId) return;

    if (!pendingAction.value) {
        pendingAction.value = { devId, port };
        showNotification(`Порт ${port} выбран.`, 'info');
        return;
    }
    if (pendingAction.value.devId === devId) {
        showNotification('Петля!', 'error');
        pendingAction.value = null;
        return;
    }
    createConnection(pendingAction.value.devId, pendingAction.value.port, devId, port, currentMode.value);
    pendingAction.value = null;
}

function createConnection(id1, p1, id2, p2, type) {
    devices[id1].usedPorts[p1] = true;
    devices[id2].usedPorts[p2] = true;
    const isValid = validateCable(devices[id1].type, devices[id2].type, type);
    connections.value.push({
        id: `conn-${Date.now()}`,
        from: id1,
        to: id2,
        fromPort: p1,
        toPort: p2,
        type,
        status: isValid,
    });
}

function onCableClick(connId) {
    if (currentMode.value === 'delete') deleteConnection(connId);
}

function deleteConnection(id) {
    const idx = connections.value.findIndex(x => x.id === id);
    if (idx === -1) return;
    const c = connections.value[idx];
    if (devices[c.from]) delete devices[c.from].usedPorts[c.fromPort];
    if (devices[c.to]) delete devices[c.to].usedPorts[c.toPort];
    connections.value.splice(idx, 1);
}

// --- Config modal ---
function openConfig(devId) {
    const dev = devices[devId];
    if (!dev) return;
    if (!DEVICE_CONFIG[dev.type].hasIp) {
        showNotification('Это устройство L2. Ему не нужен IP.', 'info');
        return;
    }
    editingDeviceId.value = devId;
}

function closeConfig() {
    editingDeviceId.value = null;
}

function saveConfig({ ip, mask, gw }) {
    const devId = editingDeviceId.value;
    if (!devId) return;

    if (ip && !IP_REGEX.test(ip)) { showNotification('Неверный IP! Пример: 192.168.1.1', 'error'); return; }
    if (mask && !IP_REGEX.test(mask)) { showNotification('Неверная маска!', 'error'); return; }
    if (gw && !IP_REGEX.test(gw)) { showNotification('Неверный шлюз!', 'error'); return; }

    if (ip) {
        const duplicate = Object.values(devices).find(d => d.ip === ip && d.id !== devId);
        if (duplicate) { showNotification(`IP ${ip} уже занят!`, 'error'); return; }
    }

    devices[devId].ip = ip;
    devices[devId].mask = mask;
    devices[devId].gw = gw;
    closeConfig();
}

// --- Ping ---
async function handlePing(id) {
    if (!pendingAction.value) {
        pendingAction.value = { devId: id };
        showNotification('Откуда? Выбери куда.', 'info');
        return;
    }
    if (pendingAction.value.devId !== id) {
        await startPing(pendingAction.value.devId, id);
    }
    pendingAction.value = null;
}

async function startPing(srcId, dstId) {
    const d1 = devices[srcId];
    const d2 = devices[dstId];
    if (!d1.ip || !d2.ip) { showNotification('Нет IP!', 'error'); return; }

    const ip1 = ipToLong(d1.ip);
    const ip2 = ipToLong(d2.ip);
    const maskLen1 = getMaskLength(d1.mask);
    const mask1 = maskLen1 === 0 ? 0 : (~0 << (32 - maskLen1)) >>> 0;
    const netId1 = (ip1 & mask1) >>> 0;
    const netId2 = (ip2 & mask1) >>> 0;

    const path = findPathBFS(srcId, dstId, devices, connections.value);
    if (!path) { showNotification('Request Timed Out (нет физического пути)', 'error'); return; }

    if (netId1 !== netId2) {
        if (d2.type !== 'router') {
            if (!d1.gw) { showNotification('Разные подсети, шлюз не задан!', 'error'); return; }
            const hasRouter = path.some(nodeId => devices[nodeId].type === 'router');
            if (!hasRouter) { showNotification('Узлы в разных сетях, нужен роутер!', 'error'); return; }
        }
    }

    packet.visible = true;
    for (let i = 0; i < path.length - 1; i++) {
        await animatePacket(devices[path[i]], devices[path[i + 1]]);
    }
    for (let i = path.length - 1; i > 0; i--) {
        await animatePacket(devices[path[i]], devices[path[i - 1]]);
    }
    packet.visible = false;
    showNotification('Ping Successful!', 'success');
    hasSuccessfulPing.value = true;
}

async function animatePacket(d1, d2) {
    const steps = 20;
    const dx = (d2.x - d1.x) / steps;
    const dy = (d2.y - d1.y) / steps;
    for (let i = 0; i <= steps; i++) {
        packet.x = d1.x + 35 + dx * i;
        packet.y = d1.y + 35 + dy * i;
        await new Promise(r => setTimeout(r, 10));
    }
}

// --- Notifications ---
function showNotification(text, type = 'info') {
    const colorMap = { error: '#ef4444', success: '#22c55e', info: '#4f46e5' };
    notificationKey.value++;
    notification.value = { text, color: colorMap[type] || colorMap.info };
    clearTimeout(notifTimeout);
    notifTimeout = setTimeout(() => { notification.value = null; }, 5000);
}

function hideNotification() {
    notification.value = null;
}

function replayAnimation() {
    if (!notification.value) return;
    notificationKey.value++;
    clearTimeout(notifTimeout);
    notifTimeout = setTimeout(() => { notification.value = null; }, 5000);
}

// --- Workspace bg click ---
function onWorkspaceBgClick(e) {
    if (e.target === workspaceEl.value || e.target.classList?.contains('connections-layer')) {
        hideNotification();
        closeConfig();
    }
}

// --- Solution check ---
function checkSolution() {
    if (!currentCriteria.value) return;
    const counts = {};
    for (const d of Object.values(devices)) {
        counts[d.type] = (counts[d.type] || 0) + 1;
    }

    for (const t in (currentCriteria.value.devices || {})) {
        if ((counts[t] || 0) < currentCriteria.value.devices[t]) {
            showNotification(`Не хватает: ${t.toUpperCase()}`, 'error');
            return;
        }
    }

    if (currentCriteria.value.connections > 0) {
        const validLinks = connections.value.filter(c => {
            if (!c.status) return false;
            if (currentCriteria.value.ignoreLinkStatus) return true;
            if (devices[c.from].type === 'router' && !devices[c.from].ip) return false;
            if (devices[c.to].type === 'router' && !devices[c.to].ip) return false;
            return true;
        });
        if (validLinks.length < currentCriteria.value.connections) {
            showNotification('Проверь кабели/лампочки.', 'error');
            return;
        }
    }

    if (currentCriteria.value.requireIp) {
        const pcs = Object.values(devices).filter(d => d.type === 'pc' || d.type === 'router');
        if (pcs.some(pc => !pc.ip)) {
            showNotification('Настрой IP адреса!', 'error');
            return;
        }
    }

    if (currentCriteria.value.requirePing && !hasSuccessfulPing.value) {
        showNotification('Сделай успешный PING!', 'error');
        return;
    }

    showNotification('Отлично! Задание выполнено.', 'success');
    saveProgress(currentTaskId.value);
}

function saveProgress(taskId) {
    const cur = readCookie('CiscoProgress') || '';
    const arr = cur.split(',').filter(Boolean);
    if (!arr.includes(taskId.toString())) {
        arr.push(taskId.toString());
        writeCookie('CiscoProgress', arr.join(','), 365);
        completedTaskIds.value = new Set(arr.map(Number));
    }
}

// --- Lifecycle ---
onMounted(() => {
    // Load completed task IDs from cookie
    const cookie = readCookie('CiscoProgress');
    if (cookie) {
        completedTaskIds.value = new Set(cookie.split(',').filter(Boolean).map(Number));
    }

    // Auto-select first task
    if (props.tasks.length > 0) selectTask(props.tasks[0].Id);
});

onBeforeUnmount(() => {
    clearTimeout(notifTimeout);
    clearTimeout(longPressTimer);
    document.removeEventListener('mousemove', onMouseMove);
    document.removeEventListener('mouseup', onMouseUp);
    document.removeEventListener('touchmove', onTouchMove);
    document.removeEventListener('touchend', onTouchEnd);
    document.removeEventListener('touchmove', onTouchDragMove);
    document.removeEventListener('touchend', onTouchDragEnd);
    if (touchGhost) touchGhost.remove();
});
</script>
