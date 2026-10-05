<script setup>
import { computed } from 'vue'
import { useGame } from '../stores/game'
import TokenIcon from './TokenIcon.vue'
const props = defineProps({ square: Object, detailed: Boolean, highlightRent: Boolean })
const game = useGame()
const deed = computed(() => game.deed(props.square.id))
const owner = computed(() => game.player(deed.value?.ownerId))
const railwayOwners = computed(() => {
  if (props.square.type !== 'rail') return []
  return game.state.players.map(player => ({
    player,
    count:game.state.deeds.filter(deed => deed.ownerId === player.id && game.square(deed.squareId)?.type === 'rail').length
  })).filter(entry => entry.count > 0).sort((a,b) => b.count - a.count)
})
const ownsFullGroup = computed(() => {
  if (!owner.value || props.square.type !== 'street') return false
  const group = game.state.board.filter(square => square.type === 'street' && square.group === props.square.group)
  return group.length > 0 && group.every(square => game.deed(square.id)?.ownerId === owner.value.id)
})
const currentRentKey = computed(() => {
  if (deed.value?.mortgaged) return null
  const owned = game.state.deeds.filter(d => d.ownerId && d.ownerId === deed.value?.ownerId)
  if (props.square.type === 'street') {
    if (deed.value?.buildings) return `building-${deed.value.buildings}`
    const group = game.state.board.filter(s => s.type === 'street' && s.group === props.square.group)
    return owner.value && group.every(s => owned.some(d => d.squareId === s.id)) ? 'group' : 'base'
  }
  if (props.square.type === 'rail') return `rail-${Math.max(1, owned.filter(d => game.square(d.squareId)?.type === 'rail').length)}`
  const diceTotal = (game.state.dice?.die1 ?? 0) + (game.state.dice?.die2 ?? 0)
  const freshRoll = game.state.dice?.utility && game.active?.position === props.square.id
  const both = owned.filter(d => game.square(d.squareId)?.type === 'utility').length === 2
  return freshRoll || both || (diceTotal > 0 && game.state.rentDue === diceTotal * 10 * game.state.settings.scale && game.active?.position === props.square.id) ? 'utility-10' : 'utility-4'
})
const rentRows = computed(() => {
  const square = props.square
  let rows
  if (square.type === 'street') rows = [
    { key:'base', label:game.t('rentBase'), amount:square.rents[0] },
    { key:'group', label:game.t('rentGroup'), amount:square.rents[0] * 2 },
    ...['house1','house2','house3','house4','hotelRent'].map((label,i) => ({ key:`building-${i+1}`, label:game.t(label), amount:square.rents[i+1] }))
  ]
  else if (square.type === 'rail') rows = square.rents.map((amount,i) => ({ key:`rail-${i+1}`, label:`${i+1} 🚂 ${game.t(i === 0 ? 'stationSingle' : 'stationPlural')}`, amount }))
  else rows = [4,10].map(factor => ({ key:`utility-${factor}`, label:game.t(factor === 4 ? 'utilitySingleRent' : 'utilityBothRent'), textOnly:true }))
  return rows.map(row => {
    const current = props.highlightRent && row.key === currentRentKey.value
    const due = current && game.active?.position === square.id && game.state.phase === 'rent' ? game.state.rentDue : 0
    return { ...row, current, value:due > 0 ? game.money(due) : row.textOnly ? '' : game.money(row.amount) }
  })
})
</script>

<template>
  <article class="deed" :class="{ detailed, 'is-mortgaged':deed?.mortgaged }" :style="{'--property-color': square.color || '#efe9d8'}">
    <header class="deed-title"><small>{{ game.t('titleDeed') }}</small><strong>{{ square.name }}</strong></header>
    <div v-if="square.type !== 'street'" class="deed-symbol">{{ square.type === 'rail' ? '🚂' : square.id === 12 ? '💡' : '🚰' }}</div>
    <div class="deed-body">
      <div v-if="!detailed && square.rents" class="deed-rent"><span>{{ game.t('rent') }}</span><b>{{ game.money(square.rents[0]) }}</b></div>
      <template v-if="detailed">
        <p v-if="square.type === 'rail'" class="railway-explanation">{{ game.t('railwayRent') }}</p>
        <div v-for="row in rentRows" :key="row.key" class="rent-row" :class="{'current-rent':row.current}"><span>{{ row.label }}</span><span v-if="row.value">{{ row.value }}</span></div>
        <template v-if="square.type === 'street'">
          <hr><div><span>{{ game.t('cost') }}</span><b>{{ game.money(square.buildCost) }}</b></div>
        </template>
        <hr><div><span>{{ game.t('mortgageValue') }}</span><b>{{ game.money(square.mortgage) }}</b></div>
        <p>{{ game.t('owner') }}: <b>{{ owner?.name || game.t('bank') }}</b></p>
        <p v-if="ownsFullGroup" class="full-group-note">🏘️ {{ game.t('ownsFullGroup').replace('{name}', owner.name) }}</p>
        <section v-if="railwayOwners.length" class="railway-owners">
          <h3>{{ game.t('railwayOwners') }}</h3>
          <ul>
            <li v-for="entry in railwayOwners" :key="entry.player.id">
              <TokenIcon :token="entry.player.token" /><strong>{{ entry.player.name }}</strong>
              <span>{{ entry.count }} 🚂 {{ game.t(entry.count === 1 ? 'stationSingle' : 'stationPlural') }}</span>
            </li>
          </ul>
        </section>
      </template>
      <p v-if="deed?.mortgaged" class="mortgage-mark">{{ game.t('mortgaged') }}</p>
      <span v-if="deed?.buildings" class="building-mark">{{ deed.buildings === 5 ? '🏨' : '⌂'.repeat(deed.buildings) }}</span>
      <footer>{{ game.money(square.price) }}</footer>
    </div>
  </article>
</template>

<style scoped>
.rent-row{font-weight:400;text-align:left;align-items:baseline}
.rent-row>span+span{white-space:nowrap}
.rent-row.current-rent{font-weight:800;background:#e1ead8;border-radius:4px;padding:5px 4px;margin-inline:-4px}
.full-group-note{background:#e1ead8;border:1px solid #b9cdb1;border-radius:6px;padding:8px;font-weight:700;text-align:center;line-height:1.4}
.railway-explanation{line-height:1.5;margin:0 0 12px}
.railway-owners{border-top:1px solid var(--line);padding-top:12px;margin-bottom:12px}
.railway-owners h3{font-size:inherit;margin:0 0 8px}
.railway-owners ul{list-style:none;margin:0;padding:0;display:grid;gap:8px}
.railway-owners li{display:flex;align-items:center;gap:6px;text-align:left}
.railway-owners strong{min-width:0;overflow-wrap:anywhere}
.railway-owners li>span{margin-left:auto;white-space:nowrap}
.railway-owners :deep(.token-figure){width:24px;height:24px;flex-shrink:0}
</style>
