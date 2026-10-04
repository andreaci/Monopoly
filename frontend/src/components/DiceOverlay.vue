<script setup>
import { computed } from 'vue'
import { useGame } from '../stores/game'
import TokenIcon from './TokenIcon.vue'
defineProps({ phone:Boolean })
const game = useGame()
const rolling = computed(()=>game.state?.phase === 'rolling' && game.state.dice)
const remaining = computed(()=> Math.max(0,Date.parse(game.state.dice?.endsAt)-game.now))
const result = computed(()=>remaining.value < 650)
const visibleDice = computed(()=>result.value ? [game.state.dice.die1,game.state.dice.die2] : [1+Math.floor(game.now/90)%6,1+Math.floor(game.now/130)%6])
const pipMap = {1:[5],2:[1,9],3:[1,5,9],4:[1,3,7,9],5:[1,3,5,7,9],6:[1,3,4,6,7,9]}
</script>
<template>
  <Transition name="fade"><div v-if="rolling" class="dice-overlay" :class="{'phone-dice':phone}" role="status" aria-live="polite">
    <span class="eyebrow">{{ game.t('turn') }}</span><h2><TokenIcon :token="game.player(game.state.dice.playerId)?.token" /> {{ game.player(game.state.dice.playerId)?.name }}</h2>
    <div class="dice-pair"><div v-for="(die,index) in visibleDice" :key="index" class="die" :class="{tumbling:!result}"><i v-for="n in 9" :key="n" :class="{pip:pipMap[die].includes(n)}"></i></div></div>
    <strong class="dice-total">{{ result ? visibleDice[0]+visibleDice[1] : game.t('rolling') }}</strong>
  </div></Transition>
</template>
