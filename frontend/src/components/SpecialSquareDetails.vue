<script setup>
import { computed } from 'vue'
import { useGame } from '../stores/game'
import { squareSymbol } from './squareSymbol'
const props = defineProps({ square:Object })
const game = useGame()
const description = computed(() => {
  const type = props.square.type
  const key = ['jail','goToJail'].includes(type) && !game.state.settings.jail ? `${type}DisabledDetails` : `${type}Details`
  return game.t(key).replace('{amount}', game.money(type === 'go' ? game.state.settings.goPayment : props.square.price || 0))
})
</script>

<template>
  <article class="special-square-details">
    <div class="square-illustration" :class="square.type">
      <h2>{{ square.name }}</h2>
      <span class="illustration-symbol" aria-hidden="true">{{ squareSymbol(square) }}</span>
      <strong v-if="square.type==='go'" class="square-amount">+ {{ game.money(game.state.settings.goPayment) }}</strong>
      <strong v-else-if="square.type==='tax'" class="square-amount">− {{ game.money(square.price) }}</strong>
    </div>
    <p class="square-explanation">{{ description }}</p>
    <p class="square-owner">{{ game.t('owner') }}: <b>{{ game.t('noSquareOwner') }}</b></p>
  </article>
</template>

<style scoped>
.special-square-details{text-align:center;padding-top:26px}
.square-illustration{width:min(100%,240px);min-height:240px;margin:0 auto;display:flex;flex-direction:column;align-items:center;justify-content:space-between;gap:20px;padding:22px 15px;background:#e1ebd7;border:2px solid #213b30;box-shadow:4px 4px 0 #213b301c;color:#213b30}
.square-illustration h2{font-size:18px;line-height:1.25;text-transform:uppercase;margin:0;overflow-wrap:anywhere}
.illustration-symbol{font-size:90px;line-height:1;margin:auto 0}
.chance .illustration-symbol{font-weight:700;color:#bc672f;text-shadow:1px 1px #222}
.go .illustration-symbol{color:#c73935;font-size:120px}
.jail{background:linear-gradient(135deg,#e9e9d4 30%,#e7a158 30%,#e7a158 83%,#e9e9d4 83%)}
.square-amount{font-size:19px}.square-explanation{font-size:15px;line-height:1.65;margin:24px 0 0}
.square-owner{font-size:13px;color:var(--muted);margin:16px 0 0}
</style>
