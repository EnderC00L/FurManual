import { createApp } from 'vue';
import CiscoSimulator from './CiscoSimulator.vue';

const mount = document.getElementById('ciscoSimulator');
if (mount) {
    let tasks = [];
    const tasksJsonEl = document.getElementById('ciscoTasksJson');
    if (tasksJsonEl) {
        try { tasks = JSON.parse(tasksJsonEl.textContent || '[]'); }
        catch (err) { console.error('Failed to parse cisco tasks JSON:', err); }
    }
    createApp(CiscoSimulator, { tasks }).mount(mount);
}
