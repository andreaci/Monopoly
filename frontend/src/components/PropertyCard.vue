<script setup>
import { computed } from 'vue'
import { useGame } from '../stores/game'
const props = defineProps({ square: Object, detailed: Boolean })
const game = useGame()
const deed = computed(() => game.deed(props.square.id))
const owner = computed(() => game.player(deed.value?.ownerId))
</script>

<template>
  <article class="deed" :class="{ detailed, 'is-mortgaged':deed?.mortgaged }" :style="{'--property-color': square.color || '#efe9d8'}">
    <header class="deed-title"><small>{{ game.t('titleDeed') }}</small><strong>{{ square.name }}</strong></header>
    <div v-if="square.type !== 'street'" class="deed-symbol">{{ square.type === 'rail' ? '🚂' : square.id === 12 ? '💡' : '🚰' }}</div>
    <div class="deed-body">
      <div v-if="square.rents" class="deed-rent"><span>{{ game.t('rent') }}</span><b>{{ game.money(square.rents[0]) }}</b></div>
      <template v-if="detailed">
        <template v-if="square.type === 'street'">
          <div><span>{{ game.t('rentGroup') }}</span><b>{{ game.money(square.rents[0] * 2) }}</b></div>
          <div v-for="(label, index) in ['house1','house2','house3','house4','hotelRent']" :key="label"><span>{{ game.t(label) }}</span><b>{{ game.money(square.rents[index+1]) }}</b></div>
          <hr><div><span>{{ game.t('cost') }}</span><b>{{ game.money(square.buildCost) }}</b></div>
        </template>
        <template v-else-if="square.type === 'rail'"><p>{{ game.t('railwayRent') }}</p><div v-for="(rent,i) in square.rents" :key="i"><span>{{ i+1 }} 🚂</span><b>{{ game.money(rent) }}</b></div></template>
        <p v-else>{{ game.t('utilityRent') }}</p>
        <hr><div><span>{{ game.t('mortgageValue') }}</span><b>{{ game.money(square.mortgage) }}</b></div>
        <p v-if="owner">{{ game.t('owner') }}: <b>{{ owner.name }}</b></p>
      </template>
      <p v-if="deed?.mortgaged" class="mortgage-mark">{{ game.t('mortgaged') }}</p>
      <span v-if="deed?.buildings" class="building-mark">{{ deed.buildings === 5 ? '🏨' : '⌂'.repeat(deed.buildings) }}</span>
      <footer>{{ game.money(square.price) }}</footer>
    </div>
  </article>
</template>
