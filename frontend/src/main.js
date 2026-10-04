import { createApp } from 'vue'
import { createPinia } from 'pinia'
import { createRouter, createWebHistory } from 'vue-router'
import App from './App.vue'
import './styles.css'

const router = createRouter({ history: createWebHistory(import.meta.env.BASE_URL), routes: ['/', '/join', '/play', '/display'].map(path => ({ path, component: { template: '<div />' } })) })
router.beforeEach(to => {
  const match = new URLSearchParams(location.search).get('match')
  if (match && !to.query.match) return { ...to, query: { ...to.query, match } }
})
createApp(App).use(createPinia()).use(router).mount('#app')
