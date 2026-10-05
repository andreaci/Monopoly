<script setup>
import { computed } from 'vue'
import { useGame } from '../stores/game'
import { squareSymbol } from './squareSymbol'
const props = defineProps({ player: Object })
defineEmits(['select'])
const game = useGame()
const square = computed(() => {
  const landing = game.state.landing
  if (landing?.playerId === props.player.id && landing.path?.length) {
    const step = Math.min(landing.path.length - 1, Math.max(0, Math.floor((game.now - Date.parse(landing.movementStartsAt)) / 200)))
    return game.square(landing.path[step])
  }
  return game.square(props.player.position)
})
const symbol = computed(() => square.value && squareSymbol(square.value))
</script>

<template>
  <button v-if="square" class="position-card" :title="`${game.t('position')}: ${square.name}`" :aria-label="`${game.t('position')}: ${square.name}`" @click="$emit('select', square)">
    <span class="position-band" :style="{background:square.color || '#e0e7da'}"></span>
    <strong>{{ square.name }}</strong>
    <span v-if="symbol" class="position-symbol">{{ symbol }}</span>
    <small v-else>{{ game.money(square.price) }}</small>
  </button>
</template>

<style scoped>
.position-card{width:42px;height:58px;flex:0 0 42px;padding:3px;border:1px solid #615d4b;border-radius:2px;background:#faf5e3;display:flex;flex-direction:column;align-items:center;gap:3px;color:#302e23}
.position-band{width:100%;height:8px;flex-shrink:0;border:1px solid #615d4b}
.position-card strong{font-size:6px;line-height:1.2;text-align:center;text-transform:uppercase;overflow-wrap:anywhere}
.position-symbol{font-size:12px;line-height:1;margin-top:auto}
.position-card small{font-size:6px;margin-top:auto}
</style>
