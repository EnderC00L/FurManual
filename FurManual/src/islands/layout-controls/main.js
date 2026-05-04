// Layout-уровневые обработчики, ранее жившие инлайн в _Layout.cshtml:
// - мобильное меню (бургер ↔ .sidebar)
// - пасхалка «Логово разработчика» (10 кликов по заголовку «Администраторам»)

const ADMIN_CLICK_THRESHOLD = 10;
const ADMIN_CLICK_RESET_MS = 1000;

function setupMobileMenu() {
    const menuToggle = document.getElementById('menuToggle');
    const sidebar = document.querySelector('.sidebar');
    const content = document.querySelector('.content');

    if (menuToggle && sidebar) {
        menuToggle.addEventListener('click', () => {
            sidebar.classList.toggle('active');
        });
    }

    if (content && sidebar) {
        content.addEventListener('click', () => {
            sidebar.classList.remove('active');
        });
    }
}

function setupDevLair() {
    const adminTrigger = document.getElementById('adminTrigger');
    const devLair = document.getElementById('devLair');
    const devClose = document.getElementById('devLairClose');
    if (!devLair) return;

    const open = () => devLair.classList.add('active');
    const close = () => devLair.classList.remove('active');

    if (adminTrigger) {
        let clicks = 0;
        let resetTimer;
        adminTrigger.addEventListener('click', () => {
            clicks++;
            clearTimeout(resetTimer);
            resetTimer = setTimeout(() => { clicks = 0; }, ADMIN_CLICK_RESET_MS);
            if (clicks >= ADMIN_CLICK_THRESHOLD) {
                open();
                clicks = 0;
                clearTimeout(resetTimer);
            }
        });
    }

    if (devClose) {
        devClose.addEventListener('click', close);
    }

    document.addEventListener('keydown', (e) => {
        if (e.key === 'Escape' && devLair.classList.contains('active')) close();
    });

    document.addEventListener('click', (e) => {
        if (!devLair.classList.contains('active')) return;
        const target = e.target;
        if (target === devLair || devLair.contains(target)) return;
        if (adminTrigger && (target === adminTrigger || adminTrigger.contains(target))) return;
        close();
    });
}

function init() {
    setupMobileMenu();
    setupDevLair();
}

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init);
} else {
    init();
}
