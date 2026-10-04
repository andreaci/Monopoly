import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

export default defineConfig({
  base: process.env.VITE_BASE_PATH || '/',
  plugins: [vue()],
  server: { proxy: { '/api': 'http://localhost:5080', '/hubs': { target: 'http://localhost:5080', ws: true } } },
})
