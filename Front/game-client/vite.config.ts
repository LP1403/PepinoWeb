import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
export default defineConfig({
    plugins: [react()],
    server: {
        host: '0.0.0.0',
        port: 5174,
        strictPort: true,
        proxy: { '/gamehub': { target: 'http://localhost:5264', ws: true }, '/api': 'http://localhost:5264' }
    },
    build: { rollupOptions: { output: { manualChunks: { three: ['three'] } } } }
});
