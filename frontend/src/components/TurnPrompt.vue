<script setup>
import { computed } from 'vue'
import { useGame } from '../stores/game'
import TokenIcon from './TokenIcon.vue'
const game = useGame()
const visible = computed(() => game.state?.phase === 'ready' && game.active?.id === game.meId && game.can('roll'))
</script>
<template>
  <Teleport to="body"><Transition name="prompt"><section v-if="visible" class="turn-prompt" role="region" :aria-label="game.t('rollPrompt')">
    <div class="turn-prompt-heading"><TokenIcon :token="game.me?.token" /><div><strong>{{ game.t('rollPrompt') }}</strong><span>{{ game.me?.name }}</span></div></div>
    <button :disabled="!game.can('roll')" @click="game.command('roll')">{{ game.t('roll') }} <span aria-hidden="true">⚄</span></button>
  </section></Transition></Teleport>
</template>
<style scoped>
.turn-prompt{position:fixed;bottom:max(18px,env(safe-area-inset-bottom));left:50%;transform:translateX(-50%);width:min(360px,calc(100vw - 32px));z-index:65;background:#17382b;color:#fff;border:1px solid #668b70;border-radius:16px;padding:20px;box-shadow:0 12px 40px #10291f66}.turn-prompt-heading{display:flex;gap:14px;align-items:center;margin-bottom:16px}.turn-prompt-heading>.token-figure{font-size:26px}.turn-prompt-heading strong{display:block;font-size:22px;line-height:1.3}.turn-prompt-heading span{display:block;font-size:14px;color:#d6e4d4;margin-top:4px}.turn-prompt button{width:100%;font-size:18px;background:#f5efdc;color:#17382b;display:flex;align-items:center;justify-content:center;gap:12px}.turn-prompt button span{font-size:24px}.prompt-enter-active,.prompt-leave-active{transition:opacity .2s,translate .2s}.prompt-enter-from,.prompt-leave-to{opacity:0;translate:0 20px}@media(prefers-reduced-motion:reduce){.prompt-enter-active,.prompt-leave-active{transition:none}}
</style>
