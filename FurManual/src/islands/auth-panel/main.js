import { createApp } from 'vue';
import AuthPanel from './AuthPanel.vue';

const mountPoint = document.getElementById('authPanel');
if (mountPoint) {
    createApp(AuthPanel).mount(mountPoint);
}
