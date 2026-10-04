<script setup>
import { computed } from 'vue'
import { useGame } from '../stores/game'
import TokenIcon from './TokenIcon.vue'
defineProps({ phone:Boolean })
const game = useGame()
const rolling = computed(()=>game.state?.phase === 'rolling' && game.state.dice)
const landing = computed(()=>game.state?.landing && game.now >= Date.parse(game.state.landing.startedAt) ? game.state.landing : null)
const landingPlayer = computed(()=>game.player(landing.value?.playerId))
const finished = computed(()=>landing.value?.autoAdvance && game.now >= Date.parse(landing.value.locationEndsAt))
const remaining = computed(()=> Math.max(0,Date.parse(game.state.dice?.endsAt)-game.now))
const result = computed(()=>remaining.value < 650)
const visibleDice = computed(()=>result.value ? [game.state.dice.die1,game.state.dice.die2] : [1+Math.floor(game.now/90)%6,1+Math.floor(game.now/130)%6])
const pipMap = {1:[5],2:[1,9],3:[1,5,9],4:[1,3,7,9],5:[1,3,5,7,9],6:[1,3,4,6,7,9]}
</script>
<template>
  <Transition name="fade"><div v-if="rolling || landing" class="dice-overlay" :class="{'phone-dice':phone}" role="status" aria-live="polite">
    <template v-if="rolling">
    <span class="eyebrow">{{ game.t('turn') }}</span><h2><TokenIcon :token="game.player(game.state.dice.playerId)?.token" /> {{ game.player(game.state.dice.playerId)?.name }}</h2>
    <div class="dice-pair"><div v-for="(die,index) in visibleDice" :key="index" class="die" :class="{tumbling:!result}"><i v-for="n in 9" :key="n" :class="{pip:pipMap[die].includes(n)}"></i></div></div>
    <strong class="dice-total">{{ result ? visibleDice[0]+visibleDice[1] : game.t('rolling') }}</strong>
    </template>
    <div v-else class="landing-reveal" :key="landing.startedAt">
      <span class="eyebrow">{{ game.t('landedAt') }}</span>
      <h2><TokenIcon :token="landingPlayer?.token" /> {{ landingPlayer?.name }}</h2>
      <div class="landing-square" :style="{'--landing-color':game.square(landing.squareId)?.color || '#c2944b'}">
        <strong>{{ game.square(landing.squareId)?.name }}</strong>
        <span v-if="landingPlayer?.inJail">{{ game.t('jailStatus') }}</span>
        <span v-else-if="landing.squareId===10">{{ game.t('visiting') }}</span>
      </div>
      <Transition name="fade" mode="out-in"><strong v-if="finished" class="landing-status" key="finished">{{ game.t(landing.extraRoll ? 'rollAgain' : 'turnFinished') }}</strong><p v-else class="landing-status" key="arrived">{{ game.t('newLocation') }}</p></Transition>
      <p v-if="game.state.waitingFor">{{ game.t('reconnect') }}: {{ game.player(game.state.waitingFor)?.name }}</p>
    </div>
  </div></Transition>
</template>
<style scoped>
.landing-reveal{text-align:center;width:min(88%,560px);animation:arrive .4s ease-out}.landing-square{background:#f8f7e9;color:#17382b;border-radius:14px;border-top:12px solid var(--landing-color);padding:clamp(20px,4vw,40px);box-shadow:0 15px 35px #0003;display:flex;flex-direction:column;gap:14px}.landing-square strong{font-size:clamp(24px,4vw,44px);line-height:1.2;overflow-wrap:anywhere}.landing-square span{font-size:16px}.landing-status{display:block;min-height:32px;margin:25px 0 0;font-size:22px}.landing-reveal>p{color:#d2e3d6}@keyframes arrive{from{opacity:0;transform:translateY(20px) scale(.95)}to{opacity:1;transform:none}}@media(prefers-reduced-motion:reduce){.landing-reveal{animation:none}}
</style>
