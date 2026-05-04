import { createApp } from 'vue';
import FileGridController from '../../components/FileGridController.vue';

const mount = document.getElementById('practicalWorksControls');
if (mount) {
    createApp(FileGridController, {
        config: {
            containerId: 'practicalWorksContainer',
            pageSelector: '.practical-works-page',
            gridSelector: '.practical-works-grid',
            dataPrefix: 'pw',
            formIds: {
                details: 'practicalWorkFormDetails',
                hiddenId: 'hiddenPracticalWorkId',
                titleInput: 'practicalWorkTitleInput',
                fileInput: 'practicalWorkFileInput',
            },
            texts: {
                addTitle: 'Добавить новую практическую работу',
                editTitle: 'Редактировать практическую работу',
            },
        },
        initialSearchTerm: mount.dataset.initialSearchTerm || '',
        initialSortOrder: mount.dataset.initialSortOrder || 'newest',
    }).mount(mount);
}
