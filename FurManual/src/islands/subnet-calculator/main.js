import { createApp } from 'vue';
import SubnetCalculator from './SubnetCalculator.vue';

const mount = document.getElementById('subnetCalculator');
if (mount) {
    createApp(SubnetCalculator).mount(mount);
}
