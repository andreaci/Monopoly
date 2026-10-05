<script setup>
import { computed } from 'vue'
import { useGame } from '../stores/game'
import PropertyCard from './PropertyCard.vue'
import TokenIcon from './TokenIcon.vue'
const game = useGame()
const purchasing = computed(()=>game.state?.phase === 'purchase')
const visible = computed(()=>['purchase','rent'].includes(game.state?.phase) && game.active?.id === game.meId && !game.state.landing)
const property = computed(()=>game.square(game.me?.position))
const owner = computed(()=>game.player(game.deed(property.value?.id)?.ownerId))
const affordable = computed(()=>game.me?.cash >= property.value?.price)
</script>
<template>
  <Teleport to="body"><Transition name="decision"><section v-if="visible && property" class="property-decision" role="dialog" aria-modal="true" aria-labelledby="property-decision-name">
    <div class="decision-content">
      <span class="eyebrow">{{ game.t(purchasing ? 'purchase' : 'rentPhase') }}</span>
      <h1 id="property-decision-name">{{ property.name }}</h1>
      <p class="decision-owner">{{ game.t('owner') }}: <TokenIcon v-if="owner" :token="owner.token" /><strong>{{ owner?.name || game.t('bank') }}</strong></p>
      <PropertyCard :square="property" detailed highlight-rent />
      <p v-if="!purchasing" class="decision-rent">{{ game.t('rent') }}: <strong>{{ game.money(game.state.rentDue) }}</strong></p>
      <p class="decision-balance">{{ game.t('money') }}: <strong>{{ game.money(game.me.cash) }}</strong></p>
      <p v-if="purchasing && !affordable" class="decision-warning">{{ game.t('cannotAffordProperty') }}</p>
      <div class="decision-buttons">
        <template v-if="purchasing">
          <button class="primary" :disabled="!game.can('buy') || !affordable" @click="game.command('buy')"><strong>{{ game.t('buy') }}</strong><span>{{ game.money(property.price) }}</span></button>
          <button :disabled="!game.can('declineEndTurn')" @click="game.command('declineEndTurn')">{{ game.t(game.state.settings.bankRent ? 'declinePayRent' : 'declineEndTurn') }}</button>
        </template>
        <template v-else>
          <button class="primary" :disabled="!game.can('payRent')" @click="game.command('payRent')"><strong>{{ game.t('payRent') }}</strong><span>{{ game.money(game.state.rentDue) }}</span></button>
          <button v-if="game.actions.includes('requestAuction')" :disabled="!game.can('requestAuction')" @click="game.command('requestAuction')">{{ game.t('offerToBuy') }}</button>
        </template>
      </div>
      <p v-if="!purchasing && game.actions.includes('requestAuction')" class="fineprint">{{ game.t('auctionHint') }}</p>
      <p v-if="game.error" role="alert" class="decision-warning">{{ game.error }}</p>
    </div>
  </section></Transition></Teleport>
</template>
<style scoped>
.property-decision{position:fixed;inset:0;z-index:80;overflow:auto;display:flex;justify-content:center;padding:24px 20px max(24px,env(safe-area-inset-bottom));background:#f8f7ec;color:#17382b}
.decision-content{width:min(100%,560px);margin:auto;text-align:center}.decision-content h1{font-size:clamp(26px,7vw,40px);margin:12px 0 18px}.decision-content :deep(.deed){width:min(210px,60vw);margin:0 auto}.decision-content :deep(.deed-title){min-height:48px}.decision-content :deep(.deed-title strong){font-size:16px}.decision-content :deep(.deed-title small){font-size:7px}.decision-content :deep(.deed-body){font-size:11px;padding:12px 6px}.decision-content :deep(.deed-body footer){font-size:16px}.decision-content :deep(.deed-symbol){font-size:45px}.decision-owner{display:flex;align-items:center;justify-content:center;gap:8px;margin:0 0 18px}.decision-rent{font-size:22px;margin:18px 0 0}.decision-balance{margin:18px 0}.decision-buttons{display:grid;gap:14px}.decision-buttons button{width:100%;min-height:76px;padding:18px;font-size:21px;line-height:1.35;border:2px solid #17382b;border-radius:14px;display:flex;align-items:center;justify-content:center;flex-wrap:wrap;gap:12px}.decision-buttons button span{font-size:18px}.decision-warning{color:#963b30;font-weight:600;margin:16px 0}.decision-enter-active,.decision-leave-active{transition:opacity .2s}.decision-enter-from,.decision-leave-to{opacity:0}@media(prefers-reduced-motion:reduce){.decision-enter-active,.decision-leave-active{transition:none}}
</style>

<style scoped>
.decision-content :deep(.deed.detailed){width:min(100%,340px);min-height:0;aspect-ratio:auto;padding:8px}
.decision-content :deep(.deed.detailed .deed-title){padding:12px 8px;min-height:60px}
.decision-content :deep(.deed.detailed .deed-body){font-size:13px;padding:12px 8px;gap:6px}
.decision-content :deep(.deed.detailed .deed-symbol){font-size:36px;padding:6px}
</style>
