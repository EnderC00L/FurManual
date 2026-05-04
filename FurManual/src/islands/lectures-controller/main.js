import { createApp } from 'vue';
import FileGridController from '../../components/FileGridController.vue';

const mount = document.getElementById('lecturesControls');
if (mount) {
    createApp(FileGridController, {
        config: {
            containerId: 'lecturesContainer',
            pageSelector: '.lectures-page',
            gridSelector: '.lectures-grid',
            dataPrefix: 'lecture',
            formIds: {
                details: 'lectureFormDetails',
                hiddenId: 'hiddenLectureId',
                titleInput: 'lectureTitleInput',
                fileInput: 'lectureFileInput',
            },
            texts: {
                addTitle: 'Добавить новую лекцию',
                editTitle: 'Редактировать лекцию',
            },
        },
        initialSearchTerm: mount.dataset.initialSearchTerm || '',
        initialSortOrder: mount.dataset.initialSortOrder || 'newest',
    }).mount(mount);
}
