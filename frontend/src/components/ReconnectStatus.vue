<script setup>
import { useGame } from '../stores/game'
defineProps({ player: Object })
const game = useGame()
</script>

<template>
  <span v-if="player && !player.connected" class="reconnect-status" role="status" :title="game.t('reconnect')" :aria-label="`${game.t('reconnect')}: ${player.name}`">
    <svg class="disconnect-icon" viewBox="0 0 24 24" aria-hidden="true"><path d="M5 8a11 11 0 0 1 14 0M8 12a6 6 0 0 1 8 0M11 16h2M3 3l18 18" /></svg>
    <span class="reconnect-spinner" aria-hidden="true"></span>
  </span>
</template>

<style scoped>
.reconnect-status{display:inline-flex;align-items:center;gap:5px;color:#ae6833;vertical-align:middle;flex-shrink:0}
.disconnect-icon{width:17px;height:17px;fill:none;stroke:currentColor;stroke-width:2;stroke-linecap:round}
.reconnect-spinner{width:12px;height:12px;border:2px solid #ae683344;border-top-color:currentColor;border-radius:50%;animation:reconnect-spin .85s linear infinite}
@keyframes reconnect-spin{to{transform:rotate(360deg)}}
</style>
