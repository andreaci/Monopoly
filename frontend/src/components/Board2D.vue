<script setup>
import { useGame } from '../stores/game'
import TokenIcon from './TokenIcon.vue'
const game = useGame()
defineProps({ showOwners:Boolean })
function owner(square) { return game.player(game.deed(square.id)?.ownerId) }
function playerColor(id) { return ['#bd3935','#246ba8','#a77c12','#704da0','#27764f','#b34180'][game.state.players.findIndex(p=>p.id===id)] || '#53685a' }
function movementStep(p) {
  const landing = game.state.landing
  if (landing?.playerId !== p.id || !landing.path.length) return -1
  return Math.min(landing.path.length - 1, Math.max(0, Math.floor((game.now - Date.parse(landing.movementStartsAt)) / 200)))
}
function position(p) { const step = movementStep(p); return step < 0 ? p.position : game.state.landing.path[step] }
function occupants(square) { return game.state.players.filter(p=>!p.bankrupt && position(p)===square.id) }
function hopping(p) { return movementStep(p)>0 && game.now < Date.parse(game.state.landing.startedAt) }
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
        <div v-if="square.color || showOwners && game.deed(square.id)" class="color-band" :class="{'owner-band':showOwners && game.deed(square.id)}" :style="{background:square.color || '#b7c9b0'}">
          <span v-if="showOwners && game.deed(square.id)" class="board-owner" :title="owner(square)?.name || game.t('bank')"><TokenIcon v-if="owner(square)" :token="owner(square).token" /><span v-else aria-hidden="true">▤</span><b>{{ owner(square)?.name || game.t('bank') }}</b></span>
          <span v-if="game.deed(square.id)?.buildings" class="band-buildings">{{ game.deed(square.id).buildings === 5 ? '🏨' : '⌂'.repeat(game.deed(square.id).buildings) }}</span>
        </div>
        <span class="square-name">{{ square.name }}</span>
        <span v-if="symbol(square)" class="square-symbol">{{ symbol(square) }}</span>
        <small v-if="square.price">{{ game.money(square.price) }}</small>
        <small v-if="square.type === 'go'">+ {{ game.money(game.state.settings.goPayment) }}</small>
        <span v-if="game.deed(square.id)?.mortgaged" class="board-mortgage">M</span>
      </div>
      <div class="square-tokens" :class="{'crowded-tokens':occupants(square).length>1}"><span v-for="p in occupants(square)" :key="`${p.id}-${movementStep(p)}`" class="board-token" :title="p.name" :aria-label="p.name" :class="{'active-token':p.id === game.state.activePlayerId,'hopping-token':hopping(p)}" :style="{'--player-color':playerColor(p.id)}"><TokenIcon :token="p.token" /><small class="board-token-name">{{ p.name }}</small></span></div>
      <span v-if="game.deed(square.id)?.ownerId" class="ownership-dot" :style="{background:['#d95b58','#458fc4','#d8ad43','#9072b8','#479d81','#d776a1'][game.state.players.findIndex(p=>p.id === game.deed(square.id).ownerId)]}"></span>
    </button>
  </div>
</template>
<style scoped>
.color-band.owner-band{height:32%;min-height:32%;display:flex;flex-direction:column;justify-content:center;gap:.2cqw;padding:.3cqw}
.board-owner{display:flex;align-items:center;gap:.2cqw;width:100%;min-width:0;background:#fff5e6ed;color:#17382b;border-radius:.3cqw;padding:0 .2cqw;font-size:.95cqw;line-height:1.2}
.board-owner :deep(.token-figure){width:1.6cqw;height:1.6cqw;flex-shrink:0}.board-owner b{overflow:hidden;text-overflow:ellipsis;white-space:nowrap}.band-buildings{font-size:1cqw;line-height:1}
.square-tokens{gap:.35cqw;padding:.35cqw;pointer-events:none;align-content:center}
.square-tokens>span.board-token{width:4.4cqw;height:4.4cqw;flex-shrink:0;position:relative;display:grid;place-items:center;padding:.2cqw;background:#fff9e9;border:.28cqw solid var(--player-color);border-radius:50%;box-shadow:0 .25cqw .5cqw #10251f55;font-size:1cqw}
.board-token :deep(.token-figure){width:100%;height:100%;filter:drop-shadow(0 .1cqw .1cqw #0004)}
.square-tokens>span.board-token.active-token{box-shadow:0 0 0 .2cqw #ffcf52,0 0 0 .35cqw #17382b,0 .3cqw .6cqw #0005}
.square-tokens .board-token-name{position:absolute;top:calc(100% + .25cqw);left:50%;transform:translateX(-50%);max-width:6.5cqw;background:var(--player-color);color:#fff;border-radius:.25cqw;padding:.2cqw .4cqw;font-size:.85cqw;font-weight:700;line-height:1.2;overflow:hidden;text-overflow:ellipsis;white-space:nowrap;box-shadow:0 .15cqw .3cqw #0003}
.crowded-tokens>span.board-token{width:2.65cqw;height:2.65cqw;border-width:.2cqw}.crowded-tokens .board-token-name{display:none}
.square-tokens>span.hopping-token{animation:token-hop .2s ease-in-out}@keyframes token-hop{0%{transform:translateY(.5cqw) scale(.9)}45%{transform:translateY(-1.3cqw) scale(1.1)}100%{transform:translateY(0) scale(1)}}
@media(prefers-reduced-motion:reduce){.square-tokens>span.hopping-token{animation:none}}
</style>
