// Highlight the sidebar link matching the current path.
// Replaces the logic that previously lived in wwwroot/js/site.js.

function highlightActiveLink() {
    const currentPath = window.location.pathname;
    const links = document.querySelectorAll('.sidebar-link');

    links.forEach((link) => {
        if (link.getAttribute('href') === currentPath) {
            link.classList.add('active');
        }
    });
}

if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', highlightActiveLink);
} else {
    highlightActiveLink();
}
