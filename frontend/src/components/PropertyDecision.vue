<script setup>
import { computed } from 'vue'
import { useGame } from '../stores/game'
import PropertyCard from './PropertyCard.vue'
const game = useGame()
const visible = computed(()=>game.state?.phase === 'purchase' && game.active?.id === game.meId && !game.state.landing)
const property = computed(()=>game.square(game.me?.position))
const affordable = computed(()=>game.me?.cash >= property.value?.price)
</script>
<template>
  <Teleport to="body"><Transition name="decision"><section v-if="visible && property" class="property-decision" role="dialog" aria-modal="true" :aria-label="game.t('purchase')">
    <div class="decision-content">
      <span class="eyebrow">{{ game.t('bank') }} · {{ game.t('purchase') }}</span>
      <h1>{{ property.name }}</h1>
      <PropertyCard :square="property" />
      <p class="decision-balance">{{ game.t('money') }}: <strong>{{ game.money(game.me.cash) }}</strong></p>
      <p v-if="!affordable" class="decision-warning">{{ game.t('cannotAffordProperty') }}</p>
      <div class="decision-buttons">
        <button class="primary" :disabled="!game.can('buy') || !affordable" @click="game.command('buy')"><strong>{{ game.t('buy') }}</strong><span>{{ game.money(property.price) }}</span></button>
        <button :disabled="!game.can('declineEndTurn')" @click="game.command('declineEndTurn')">{{ game.t(game.state.settings.bankRent ? 'declinePayRent' : 'declineEndTurn') }}</button>
      </div>
      <p v-if="game.error" role="alert" class="decision-warning">{{ game.error }}</p>
    </div>
  </section></Transition></Teleport>
</template>
<style scoped>
.property-decision{position:fixed;inset:0;z-index:80;overflow:auto;display:flex;justify-content:center;padding:24px 20px max(24px,env(safe-area-inset-bottom));background:#f8f7ec;color:#17382b}
.decision-content{width:min(100%,560px);margin:auto;text-align:center}.decision-content h1{font-size:clamp(26px,7vw,40px);margin:12px 0 18px}.decision-content :deep(.deed){width:150px;margin:0 auto}.decision-content :deep(.deed-title){min-height:48px}.decision-content :deep(.deed-title strong){font-size:12px}.decision-content :deep(.deed-title small){font-size:7px}.decision-content :deep(.deed-body){font-size:11px;padding:12px 6px}.decision-content :deep(.deed-body footer){font-size:16px}.decision-content :deep(.deed-symbol){font-size:45px}.decision-balance{margin:18px 0}.decision-buttons{display:grid;gap:14px}.decision-buttons button{width:100%;min-height:76px;padding:18px;font-size:21px;line-height:1.35;border:2px solid #17382b;border-radius:14px;display:flex;align-items:center;justify-content:center;flex-wrap:wrap;gap:12px}.decision-buttons button span{font-size:18px}.decision-warning{color:#963b30;font-weight:600;margin:16px 0}.decision-enter-active,.decision-leave-active{transition:opacity .2s}.decision-enter-from,.decision-leave-to{opacity:0}@media(prefers-reduced-motion:reduce){.decision-enter-active,.decision-leave-active{transition:none}}
</style>
