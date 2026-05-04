import { createApp } from 'vue';
import TestTimer from './TestTimer.vue';
import StudentNameInput from './StudentNameInput.vue';

const timerMount = document.getElementById('testTimer');
if (timerMount) {
    createApp(TestTimer).mount(timerMount);
}

const nameMount = document.getElementById('studentNameInput');
if (nameMount) {
    createApp(StudentNameInput, {
        testTitle: nameMount.dataset.testTitle || '',
        testDate: nameMount.dataset.testDate || '',
        score: nameMount.dataset.score || '',
    }).mount(nameMount);
}
