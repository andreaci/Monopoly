<script setup>
import { computed, onMounted, ref } from 'vue'
import { useGame } from '../stores/game'
import TokenIcon from './TokenIcon.vue'
defineEmits(['close'])
const game = useGame()
const closeButton = ref(null)
onMounted(() => closeButton.value?.focus())
const players = computed(() => game.state.players.map(player => {
  const deeds = game.state.deeds.filter(d => d.ownerId === player.id)
  return {
    ...player,
    wealth: player.cash + deeds.reduce((total, deed) => {
      const square = game.square(deed.squareId)
      return total + square.price - (deed.mortgaged ? square.mortgage : 0) + deed.buildings * square.buildCost
    }, 0),
    propertyCount:deeds.length,
    buildingCount:deeds.reduce((total, deed) => total + deed.buildings, 0),
    ...(player.statistics || {})
  }
}))
const awards = computed(() => [
  { icon:'💎', key:'richest', field:'wealth', money:true },
  { icon:'🪙', key:'poorest', field:'wealth', money:true, min:true },
  { icon:'💸', key:'biggestPayer', field:'paidToPlayers', money:true },
  { icon:'🧲', key:'biggestCollector', field:'receivedFromPlayers', money:true },
  { icon:'🚔', key:'jailRegular', field:'jailVisits' },
  { icon:'🏘️', key:'propertyTycoon', field:'propertyCount' },
  { icon:'🏗️', key:'masterBuilder', field:'buildingCount' },
  { icon:'🎲', key:'cardMagnet', field:'cardsDrawn' }
].map(award => {
  const values = players.value.map(p => p[award.field] || 0)
  const value = values.length ? (award.min ? Math.min(...values) : Math.max(...values)) : 0
  const winners = (value > 0 || award.field === 'wealth') ? players.value.filter(p => (p[award.field] || 0) === value) : []
  return { ...award, value, winners }
}))
</script>

<template>
  <div class="modal statistics-backdrop" @click.self="$emit('close')" @keydown.esc="$emit('close')">
    <section class="modal-content statistics-modal" role="dialog" aria-modal="true" :aria-label="game.t('statistics')" tabindex="-1">
      <button ref="closeButton" class="modal-close" @click="$emit('close')" :aria-label="game.t('close')">×</button>
      <h2>📊 {{ game.t('statistics') }}</h2>
      <div class="statistics-grid">
        <article v-for="award in awards" :key="award.key" class="statistic-award">
          <span class="award-icon" aria-hidden="true">{{ award.icon }}</span>
          <h3>{{ game.t(award.key) }}</h3>
          <div v-for="player in award.winners" :key="player.id" class="award-player"><TokenIcon :token="player.token" /><strong>{{ player.name }}</strong></div>
          <p v-if="!award.winners.length" class="muted">{{ game.t('noRecordYet') }}</p>
          <b v-else class="award-value">{{ award.money ? game.money(award.value) : award.value }}</b>
        </article>
      </div>
      <p class="fineprint">{{ game.t('statisticsHelp') }}</p>
    </section>
  </div>
</template>

<style scoped>
.statistics-modal{width:min(760px,100%);max-height:90dvh;overflow:auto}
.statistics-backdrop{z-index:2000}
.statistics-grid{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:12px}
.statistic-award{padding:16px;background:#f0f1e6;border:1px solid var(--line);border-radius:12px;text-align:center}
.award-icon{font-size:30px}.statistic-award h3{margin:8px 0 12px;font-size:14px}
.award-player{display:flex;justify-content:center;align-items:center;gap:7px;margin:5px 0;font-size:13px;overflow-wrap:anywhere}
.award-player :deep(.token-figure){width:24px;height:24px}.award-value{display:block;margin-top:10px;font-size:20px;color:var(--ink)}
.statistic-award p{font-size:12px;margin-bottom:0}
@media(max-width:400px){.statistics-grid{gap:8px}.statistic-award{padding:12px 8px}}
</style>
