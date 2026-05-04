<template>
    <div class="search-wrapper">
        <form class="search-form" @submit.prevent="submitSearch">
            <div class="input-group">
                <input type="text"
                       ref="searchInputEl"
                       v-model="searchTerm"
                       placeholder="Поиск по названию..."
                       class="search-input"
                       autocomplete="off"
                       @input="onSearchInput" />
                <button type="button"
                        class="btn-clear-search"
                        :class="{ active: hasSearch }"
                        title="Очистить"
                        @click="clearSearch">
                    <svg class="ui-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor">
                        <line x1="18" y1="6" x2="6" y2="18"></line>
                        <line x1="6" y1="6" x2="18" y2="18"></line>
                    </svg>
                </button>
            </div>
        </form>
    </div>

    <div class="filters-row">
        <div class="sort-group">
            <button type="button"
                    class="sort-btn"
                    :class="{ active: sortOrder === 'newest' }"
                    @click="changeSort('newest')">
                Сначала новые
            </button>
            <button type="button"
                    class="sort-btn"
                    :class="{ active: sortOrder === 'oldest' }"
                    @click="changeSort('oldest')">
                Сначала старые
            </button>
        </div>
    </div>
</template>

<script setup>
import { ref, computed, onMounted, onBeforeUnmount } from 'vue';

const props = defineProps({
    config: {
        type: Object,
        required: true,
        // {
        //   containerId: string,        // grid container element id
        //   pageSelector: string,       // CSS selector for the page wrapper (for scrollIntoView)
        //   gridSelector: string,       // CSS selector for the inner grid (loading class target)
        //   dataPrefix: string,         // e.g. 'lecture' -> data-lecture-action / dataset.lectureAction
        //   formIds: { details, hiddenId, titleInput, fileInput },
        //   texts: { addTitle, editTitle },
        // }
    },
    initialSearchTerm: { type: String, default: '' },
    initialSortOrder: { type: String, default: 'newest' },
});

const cfg = props.config;
const actionAttr = `data-${cfg.dataPrefix}-action`;
const datasetAction = `${cfg.dataPrefix}Action`;
const datasetId = `${cfg.dataPrefix}Id`;
const datasetTitle = `${cfg.dataPrefix}Title`;

const searchTerm = ref(props.initialSearchTerm);
const sortOrder = ref(props.initialSortOrder);
const currentPage = ref(1);

const hasSearch = computed(() => searchTerm.value.trim().length > 0);

const searchInputEl = ref(null);

let searchTimeout = null;
let gridContainer = null;
let onGridClickBound = null;
let cancelBtn = null;
let onCancelClickBound = null;
let onResizeBound = null;

let formDetails = null;
let hiddenId = null;
let titleInput = null;
let formTitle = null;
let submitText = null;
let fileHint = null;
let fileInput = null;
let fileStar = null;

function onSearchInput() {
    clearTimeout(searchTimeout);
    searchTimeout = setTimeout(submitSearch, 300);
}

function clearSearch() {
    searchTerm.value = '';
    submitSearch();
}

function submitSearch() {
    currentPage.value = 1;
    fetchGrid();
}

function changeSort(order) {
    sortOrder.value = order;
    currentPage.value = 1;
    fetchGrid();
}

function changePage(page) {
    if (page < 1) return;
    currentPage.value = page;
    fetchGrid();
    document.querySelector(cfg.pageSelector)?.scrollIntoView({ behavior: 'smooth' });
}

function buildParams() {
    const p = new URLSearchParams();
    if (searchTerm.value) p.set('SearchTerm', searchTerm.value);
    if (sortOrder.value) p.set('SortOrder', sortOrder.value);
    p.set('CurrentPage', String(currentPage.value));
    return p;
}

async function fetchGrid() {
    if (!gridContainer) return;
    gridContainer.querySelector(cfg.gridSelector)?.classList.add('loading');

    const params = buildParams();
    const fetchUrl = `${window.location.pathname}?handler=Grid&${params.toString()}`;
    const pushUrl = `${window.location.pathname}?${params.toString()}`;

    try {
        const response = await fetch(fetchUrl);
        if (!response.ok) throw new Error(response.statusText);
        const html = await response.text();
        gridContainer.innerHTML = html;
        window.history.pushState({}, '', pushUrl);
    } catch (err) {
        console.error('Ошибка загрузки:', err);
    }
}

function onGridClick(e) {
    const target = e.target.closest(`[${actionAttr}]`);
    if (!target) return;
    const action = target.dataset[datasetAction];

    if (action === 'page') {
        e.preventDefault();
        const page = parseInt(target.dataset.page, 10);
        if (!Number.isNaN(page)) changePage(page);
    } else if (action === 'copy') {
        const id = target.dataset[datasetId];
        if (id) copyDownloadLink(target, id);
    } else if (action === 'edit') {
        const id = target.dataset[datasetId];
        const title = target.dataset[datasetTitle] ?? '';
        if (id) startEdit(id, title);
    }
}

function copyDownloadLink(btn, id) {
    const url = `${window.location.origin}${window.location.pathname}?handler=Download&id=${id}`;

    navigator.clipboard.writeText(url)
        .then(() => {
            const iconDefault = btn.querySelector('.icon-default');
            const iconSuccess = btn.querySelector('.icon-success');
            if (!iconDefault || !iconSuccess) return;

            iconDefault.style.display = 'none';
            iconSuccess.style.display = 'block';
            iconSuccess.classList.add('fade-in');

            setTimeout(() => {
                iconSuccess.style.display = 'none';
                iconDefault.style.display = 'block';
                iconSuccess.classList.remove('fade-in');
            }, 2000);
        })
        .catch((err) => {
            console.error('Ошибка копирования:', err);
            alert('Не удалось скопировать ссылку');
        });
}

function startEdit(id, title) {
    if (!formDetails) return;

    formDetails.open = true;

    if (hiddenId) hiddenId.value = id;
    if (titleInput) titleInput.value = title;
    if (formTitle) formTitle.innerText = cfg.texts.editTitle;
    if (submitText) submitText.innerText = 'Сохранить изменения';
    if (fileHint) fileHint.innerText = 'Оставьте пустым, чтобы не менять текущий документ. Поддерживаются: PDF, Word.';
    if (fileInput) fileInput.required = false;
    if (fileStar) fileStar.style.display = 'none';
    if (cancelBtn) cancelBtn.style.display = 'flex';

    formDetails.scrollIntoView({ behavior: 'smooth', block: 'start' });
}

function cancelEdit() {
    if (hiddenId) hiddenId.value = '';
    if (titleInput) titleInput.value = '';
    if (formTitle) formTitle.innerText = cfg.texts.addTitle;
    if (submitText) submitText.innerText = 'Загрузить в базу';
    if (fileHint) fileHint.innerText = 'Поддерживаются: PDF, Word. Макс. размер: 50 МБ.';
    if (fileInput) {
        fileInput.value = '';
        fileInput.required = true;
    }
    if (fileStar) fileStar.style.display = 'inline';
    if (cancelBtn) cancelBtn.style.display = 'none';
}

function adaptPlaceholder() {
    if (!searchInputEl.value) return;
    searchInputEl.value.placeholder = window.innerWidth < 480 ? 'Поиск...' : 'Поиск по названию...';
}

onMounted(() => {
    formDetails = document.getElementById(cfg.formIds.details);
    hiddenId = document.getElementById(cfg.formIds.hiddenId);
    titleInput = document.getElementById(cfg.formIds.titleInput);
    formTitle = document.getElementById('formTitleText');
    submitText = document.getElementById('submitBtnText');
    fileHint = document.getElementById('fileHintText');
    fileInput = document.getElementById(cfg.formIds.fileInput);
    fileStar = document.getElementById('fileRequiredStar');
    cancelBtn = document.getElementById('cancelEditBtn');

    gridContainer = document.getElementById(cfg.containerId);
    if (gridContainer) {
        onGridClickBound = onGridClick;
        gridContainer.addEventListener('click', onGridClickBound);
    }

    if (cancelBtn) {
        onCancelClickBound = cancelEdit;
        cancelBtn.addEventListener('click', onCancelClickBound);
    }

    onResizeBound = adaptPlaceholder;
    window.addEventListener('resize', onResizeBound);
    adaptPlaceholder();
});

onBeforeUnmount(() => {
    if (gridContainer && onGridClickBound) gridContainer.removeEventListener('click', onGridClickBound);
    if (cancelBtn && onCancelClickBound) cancelBtn.removeEventListener('click', onCancelClickBound);
    if (onResizeBound) window.removeEventListener('resize', onResizeBound);
    if (searchTimeout) clearTimeout(searchTimeout);
});
</script>
