<script setup>
import { computed } from 'vue'
import { useGame } from '../stores/game'
import TokenIcon from './TokenIcon.vue'
const props = defineProps({ square:Object })
const game = useGame()
const occupants = computed(() => game.state.players.filter(player => {
  if (player.bankrupt) return false
  const landing = game.state.landing
  let position = player.position
  if (landing?.playerId === player.id && landing.path?.length) {
    const step = Math.min(landing.path.length - 1, Math.max(0, Math.floor((game.now - Date.parse(landing.movementStartsAt)) / 200)))
    position = landing.path[step]
  }
  return position === props.square.id
}))
</script>

<template>
  <section v-if="occupants.length" class="square-occupants">
    <h3>{{ game.t('playersHere') }}</h3>
    <ul>
      <li v-for="player in occupants" :key="player.id"><TokenIcon :token="player.token" /><strong>{{ player.name }}</strong></li>
    </ul>
  </section>
</template>

<style scoped>
.square-occupants{margin-top:20px;padding-top:16px;border-top:1px solid var(--line)}
.square-occupants h3{font-size:12px;color:var(--muted);text-align:center;margin:0 0 10px}
.square-occupants ul{list-style:none;padding:0;margin:0;display:flex;flex-wrap:wrap;justify-content:center;gap:8px}
.square-occupants li{display:flex;align-items:center;gap:8px;max-width:100%;padding:7px 11px;border:1px solid var(--line);border-radius:12px;background:#e5ebdf}
.square-occupants strong{font-size:13px;overflow-wrap:anywhere;min-width:0}
.square-occupants :deep(.token-figure){width:30px;height:30px;flex-shrink:0}
</style>
