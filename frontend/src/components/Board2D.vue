<script setup>
import { useGame } from '../stores/game'
import TokenIcon from './TokenIcon.vue'
const game = useGame()
defineEmits(['select'])
function cell(id) {
  if (id <= 10) return { gridRow:11, gridColumn:11-id }
  if (id <= 20) return { gridRow:21-id, gridColumn:1 }
  if (id <= 30) return { gridRow:1, gridColumn:id-19 }
  return { gridRow:id-29, gridColumn:11 }
}
function side(id) { return id % 10 === 0 ? 'corner' : id < 10 ? 'bottom' : id < 20 ? 'left' : id < 30 ? 'top' : 'right' }
function symbol(square) { return ({go:'←',jail:'▦',parking:'🚗',goToJail:'👮',chance:'?',chest:'🧰',rail:'🚂',tax:'◇',utility:square.id === 12 ? '💡' : '🚰'})[square.type] }
</script>

<template>
  <div class="board">
    <div class="board-center">
      <div class="deck-stack chest-stack"><span>🧰</span><strong>{{ game.t('chest') }}</strong></div>
      <div class="board-brand"><span>MONOPOLY</span><small>{{ game.t('table') }} · {{ game.state.players.length }} {{ game.t('players').toLowerCase() }}</small></div>
      <div class="deck-stack chance-stack"><span>?</span><strong>{{ game.t('chance') }}</strong></div>
      <div class="board-supply">{{ game.state.housesLeft }} ⌂ &nbsp; {{ game.state.hotelsLeft }} 🏨</div>
    </div>
    <button v-for="square in game.state.board" :key="square.id" class="square" :class="[side(square.id), square.type]" :style="cell(square.id)" @click.stop="$emit('select', square)">
      <div class="square-inner">
        <div v-if="square.color" class="color-band" :style="{background:square.color}"><span v-if="game.deed(square.id)?.buildings">{{ game.deed(square.id).buildings === 5 ? '🏨' : '⌂'.repeat(game.deed(square.id).buildings) }}</span></div>
        <span class="square-name">{{ square.name }}</span>
        <span v-if="symbol(square)" class="square-symbol">{{ symbol(square) }}</span>
        <small v-if="square.price">{{ game.money(square.price) }}</small>
        <small v-if="square.type === 'go'">+ {{ game.money(game.state.settings.goPayment) }}</small>
        <span v-if="game.deed(square.id)?.mortgaged" class="board-mortgage">M</span>
      </div>
      <div class="square-tokens"><span v-for="(p,index) in game.state.players.filter(p=>!p.bankrupt && p.position === square.id)" :key="p.id" :title="p.name" :class="{'active-token':p.id === game.state.activePlayerId}" :style="{'--token-offset': index}"><TokenIcon :token="p.token" /></span></div>
      <span v-if="game.deed(square.id)?.ownerId" class="ownership-dot" :style="{background:['#d95b58','#458fc4','#d8ad43','#9072b8','#479d81','#d776a1'][game.state.players.findIndex(p=>p.id === game.deed(square.id).ownerId)]}"></span>
    </button>
  </div>
</template>
