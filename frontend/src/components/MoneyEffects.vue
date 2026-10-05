<script setup>
import { onMounted, ref, watch } from 'vue'
import { useGame } from '../stores/game'

const game = useGame()
const visible = ref([])
let seen = 0
let nextId = 0

onMounted(() => {
  seen = Math.max(0, ...(game.state?.moneyEvents ?? []).map(event => event.sequence))
})

watch(() => game.state?.moneyEvents, events => {
  if (!events) return
  for (const event of events) {
    if (event.sequence <= seen) continue
    seen = event.sequence
    const effect = { ...event, id: ++nextId }
    visible.value.push(effect)
    setTimeout(() => { visible.value = visible.value.filter(item => item.id !== effect.id) }, 1900)
  }
})
</script>

<template>
  <Teleport to="body">
    <div class="money-effects" aria-live="polite" aria-atomic="false">
      <TransitionGroup name="cash-pop">
        <div v-for="event in visible" :key="event.id" class="cash-pop" :class="event.amount < 0 ? 'cash-out' : 'cash-in'">
          <strong>{{ event.amount < 0 ? '−' : '+' }}{{ game.money(Math.abs(event.amount)) }}</strong>
          <span>{{ game.player(event.playerId)?.name }}</span>
        </div>
      </TransitionGroup>
    </div>
  </Teleport>
</template>

<style scoped>
.money-effects{position:fixed;inset:0;z-index:120;display:flex;flex-direction:column;align-items:center;justify-content:center;gap:12px;pointer-events:none;overflow:hidden}
.cash-pop{display:flex;flex-direction:column;align-items:center;padding:12px 25px;border-radius:18px;background:#11271dcc;box-shadow:0 10px 35px #0005;animation:cash-float 1.8s ease-out both}
.cash-pop strong{font-size:clamp(38px,9vw,80px);font-weight:900;line-height:1.05;text-shadow:0 3px 12px #0007}
.cash-pop span{font-size:16px;font-weight:700;color:#fff;margin-top:5px}
.cash-out strong{color:#ff4b48}.cash-in strong{color:#54ed8b}
.cash-pop:nth-child(2){transform:translateY(25px);animation-delay:.12s}.cash-pop:nth-child(3){transform:translateY(50px);animation-delay:.24s}
.cash-pop-leave-active{transition:opacity .2s}.cash-pop-leave-to{opacity:0}
@keyframes cash-float{0%{opacity:0;translate:0 45px;scale:.65}12%{opacity:1;translate:0 0;scale:1}75%{opacity:1}100%{opacity:0;translate:0 -50px;scale:1.08}}
@media(prefers-reduced-motion:reduce){.cash-pop{animation:none}.cash-pop-leave-active{transition:none}}
</style>
