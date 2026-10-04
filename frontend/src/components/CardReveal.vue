<script setup>
import { computed, ref, watch } from 'vue'
import { useGame } from '../stores/game'
const game = useGame(), queue = ref([])
let matchId, seen = 0
watch(() => game.state, state => {
  if (!state || !game.meId) return
  if (matchId !== state.matchId) {
    matchId = state.matchId
    seen = Number(sessionStorage.getItem(`cards:${matchId}:${game.meId}`) || 0)
    queue.value = []
  }
  for (const event of state.cardDraws ?? []) {
    if (event.sequence > seen && event.playerId === game.meId) queue.value.push(event)
    seen = Math.max(seen, event.sequence)
  }
}, { immediate: true })
const current = computed(() => game.state?.phase === 'rolling' ? null : queue.value[0])
function dismiss() {
  const event = queue.value.shift()
  sessionStorage.setItem(`cards:${matchId}:${game.meId}`, String(event.sequence))
}
</script>
<template>
  <Teleport to="body"><Transition name="reveal"><section v-if="current" :key="current.sequence" class="card-reveal" role="dialog" aria-modal="true" :aria-label="game.t('cardDraw')">
    <p class="reveal-player">{{ game.me?.name }} · {{ game.t('cardDraw') }}</p>
    <article class="revealed-card" :class="current.card.deck"><span class="reveal-symbol">{{ current.card.deck === 'chance' ? '?' : '▦' }}</span><h1>{{ game.t(current.card.deck === 'chance' ? 'chance' : 'chest') }}</h1><p>{{ current.card.text }}</p></article>
    <button class="primary" autofocus @click="dismiss">{{ game.t('continue') }} →</button>
  </section></Transition></Teleport>
</template>
<style scoped>
.card-reveal{position:fixed;inset:0;z-index:100;display:flex;flex-direction:column;align-items:center;justify-content:center;gap:28px;padding:24px;background:radial-gradient(ellipse at center,#325b47,#10291fee);backdrop-filter:blur(12px);color:white;perspective:1000px;overflow:auto}
.reveal-player{font-size:1rem;text-align:center}.revealed-card{width:min(100%,360px);padding:35px 25px;background:#f8eed6;color:#292820;border:8px double #b69057;box-shadow:0 24px 70px #0009;border-radius:12px;text-align:center;animation:turn-card .85s cubic-bezier(.2,.75,.25,1) both}.revealed-card.chance{border-color:#da783f}.revealed-card.chest{border-color:#4f8db6}.reveal-symbol{display:block;font-size:80px;line-height:1.2;color:#d76436}.chest .reveal-symbol{color:#407caa}.revealed-card h1{font-size:25px;margin:15px 0}.revealed-card p{font-size:20px;line-height:1.6}.card-reveal button{min-width:190px;animation:reveal-button .95s both}.reveal-enter-active,.reveal-leave-active{transition:opacity .25s}.reveal-enter-from,.reveal-leave-to{opacity:0}@keyframes turn-card{from{opacity:0;transform:translateY(100px) rotateY(-150deg) scale(.7)}to{opacity:1;transform:none}}@keyframes reveal-button{0%,65%{opacity:0}100%{opacity:1}}@media(prefers-reduced-motion:reduce){.revealed-card,.card-reveal button{animation:none}.reveal-enter-active,.reveal-leave-active{transition:none}}
</style>
