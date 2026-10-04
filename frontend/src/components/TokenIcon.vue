<script setup>
import { computed, useId } from 'vue'
const props = defineProps({ token: String })
const id = useId().replace(/:/g,'')
const wood = computed(() => props.token?.startsWith('wood-'))
const paths = {
 hat:'M13 66Q4 60 15 56L24 54V22Q45 13 66 22V54L78 57Q89 65 70 69Q40 78 13 66Z',
 car:'M9 56L14 39L42 37L51 25L60 27L60 38L73 42L82 57L79 63H10Z M22 40V51M29 40V51M36 40V51',
 dog:'M13 44L5 34L12 26L18 13L24 25L33 30L62 30L72 24L78 28L71 37L69 65L60 65L58 46L35 45L30 66L20 66L22 43Z',
 cat:'M24 35L21 13L32 20L44 18L55 12L54 36Q64 38 67 51L68 63L57 65L49 48L38 48L35 65L23 65L25 40Q12 44 10 26Q10 18 17 18Q12 31 24 35Z',
 ship:'M5 50H80L66 68H22Z M23 48V31H63V48 M34 31V22H47V31 M54 31V16H63V31',
 boot:'M28 17H59L54 47Q58 52 75 52Q87 61 75 68H20L22 48Z',
 thimble:'M22 68L27 24Q28 13 43 13Q58 13 59 24L64 68Z M26 55H61 M33 24H36 M45 24H48 M33 34H36 M45 34H48 M32 44H35 M47 44H50',
 moneybag:'M29 28L23 15L35 20L43 12L50 21L63 16L57 29Q81 45 72 65Q44 80 16 65Q8 47 29 28Z M28 29H58 M37 45H50M37 56H50M41 40L38 62M49 40L46 62',
 duck:'M34 38Q22 34 23 22Q24 8 38 10Q54 11 54 26Q52 34 46 37Q49 45 66 39L77 31Q86 62 64 70Q21 79 14 56Q11 43 34 38Z M25 25L8 28L25 33',
 penguin:'M30 29Q23 24 29 16Q37 4 50 12Q58 19 55 30Q65 47 61 65L66 72L49 72L44 67L36 72H23L27 64Q21 46 30 29Z M28 20L14 24L29 28',
}
const colors = computed(() => ({'wood-orange':['#ef7e25','#258342'],'wood-mushroom':['#f4c72b','#d53a49'],'wood-pear':['#559c35','#71513f'],'wood-pawn':['#cc3246','#2c7840'],'wood-bottle':['#edc729','#cc3041'],'wood-candle':['#eec52b','#f2ead8']})[props.token] ?? ['#ddd','#aaa'])
</script>
<template>
  <svg class="token-figure" viewBox="0 0 90 85" aria-hidden="true" focusable="false">
    <defs><linearGradient :id="id" x1="0" x2="1"><stop offset="0" :stop-color="wood ? colors[0] : '#626b70'"/><stop offset=".35" :stop-color="wood ? colors[0] : '#cdd7de'"/><stop offset=".62" :stop-color="wood ? colors[0] : '#697e8b'"/><stop offset="1" :stop-color="wood ? colors[0] : '#dce2e5'"/></linearGradient></defs>
    <ellipse cx="44" cy="77" rx="33" ry="4" fill="#000" opacity=".18"/>
    <template v-if="wood">
      <path v-if="token==='wood-pear'" d="M34 25H54Q52 37 62 49Q79 76 44 76Q9 76 25 49Q36 37 34 25Z" :fill="`url(#${id})`" stroke="#25452b" stroke-width="1.5"/>
      <path v-else-if="token==='wood-candle'" d="M20 39H68L59 49V64L70 69V75H18V69L29 64V49Z" :fill="colors[0]"/>
      <path v-else d="M22 43Q12 51 18 69Q44 83 70 69Q76 51 66 43Z" :fill="`url(#${id})`" stroke="#684321" stroke-width="1.5"/>
      <path v-if="token==='wood-mushroom'" d="M12 43Q12 10 44 10Q77 10 77 43Q44 56 12 43Z" :fill="colors[1]"/>
      <path v-else-if="token==='wood-candle'" d="M34 39V17Q44 11 54 17V39Z" :fill="colors[1]"/>
      <path v-else-if="token==='wood-pawn'" d="M22 44L32 34Q9 23 34 17Q26 7 43 7Q62 7 54 17Q78 23 55 34L66 44Z" :fill="colors[1]"/>
      <path v-else d="M25 44Q20 34 34 30V16Q44 10 54 16V30Q68 34 63 44Z" :fill="colors[1]"/>
      <path v-if="token==='wood-bottle'" d="M34 17V10Q44 5 54 10V17Z" fill="#268540"/>
      <path v-if="token==='wood-candle'" d="M34 17Q35 9 44 4Q53 9 54 17Z" fill="#d83b45"/>
      <g v-if="['wood-mushroom','wood-pawn'].includes(token)" fill="#fff5df"><ellipse cx="31" cy="20" rx="5" ry="3"/><ellipse cx="55" cy="28" rx="4" ry="6"/><ellipse cx="23" cy="35" rx="3" ry="5"/></g>
      <path d="M24 53Q20 67 31 69" fill="none" stroke="white" opacity=".35" stroke-width="4" stroke-linecap="round"/>
    </template>
    <template v-else><path :d="paths[token] || paths.hat" :fill="`url(#${id})`" stroke="#263b42" stroke-width="2.8" stroke-linejoin="round"/><g v-if="token==='car'" fill="#697277" stroke="#d8e0e3" stroke-width="3"><circle cx="25" cy="62" r="10"/><circle cx="68" cy="62" r="10"/></g></template>
  </svg>
</template>
<style scoped>.token-figure{width:1.65em;height:1.65em;display:inline-block;vertical-align:middle;overflow:visible;filter:drop-shadow(0 2px 1px #0002)}</style>
