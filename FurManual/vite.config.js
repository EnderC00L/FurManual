import { defineConfig } from 'vite';
import vue from '@vitejs/plugin-vue';
import { resolve } from 'path';

// Multi-entry build: one bundle per "island". Razor Pages include only the
// scripts they need via <script type="module" src="~/js/dist/<name>.js">.
// Predictable entry filenames (no hash) — cache-busting handled by Razor's
// asp-append-version, which appends ?v=<hash> based on file contents.
export default defineConfig({
    plugins: [vue()],
    build: {
        outDir: 'wwwroot/js/dist',
        emptyOutDir: true,
        sourcemap: true,
        rollupOptions: {
            input: {
                'sidebar-active': resolve(__dirname, 'src/islands/sidebar-active/main.js'),
                'layout-controls': resolve(__dirname, 'src/islands/layout-controls/main.js'),
                'auth-panel': resolve(__dirname, 'src/islands/auth-panel/main.js'),
                'test-runner': resolve(__dirname, 'src/islands/test-runner/main.js'),
                'lectures-controller': resolve(__dirname, 'src/islands/lectures-controller/main.js'),
                'practical-works-controller': resolve(__dirname, 'src/islands/practical-works-controller/main.js'),
                'subnet-calculator': resolve(__dirname, 'src/islands/subnet-calculator/main.js'),
                'cisco-simulator': resolve(__dirname, 'src/islands/cisco-simulator/main.js'),
            },
            output: {
                entryFileNames: '[name].js',
                chunkFileNames: 'chunks/[name]-[hash].js',
                assetFileNames: 'assets/[name]-[hash][extname]',
            },
        },
    },
});
