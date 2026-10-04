<script setup>
import { ref, onBeforeUnmount } from 'vue'
import Board2D from './Board2D.vue'
import { useGame } from '../stores/game'
defineProps({ compact:Boolean })
const emit = defineEmits(['select','expand'])
const game = useGame(), scale = ref(1), x = ref(0), y = ref(0)
const pointers = new Map()
let lastDistance = 0, lastCenter, moved = false
function reset() { scale.value=1; x.value=0; y.value=0 }
function zoom(next) { scale.value=Math.max(1,Math.min(4,next)); if(scale.value===1) reset() }
function down(e) { pointers.set(e.pointerId,{x:e.clientX,y:e.clientY}); moved=false; lastDistance=0; lastCenter=null }
function move(e) {
  if(!pointers.has(e.pointerId)) return
  const old=pointers.get(e.pointerId), next={x:e.clientX,y:e.clientY}; pointers.set(e.pointerId,next)
  if(Math.hypot(next.x-old.x,next.y-old.y)>2) moved=true
  if(pointers.size===2) {
    const [a,b]=[...pointers.values()], distance=Math.hypot(a.x-b.x,a.y-b.y), center={x:(a.x+b.x)/2,y:(a.y+b.y)/2}
    if(lastDistance) zoom(scale.value*distance/lastDistance)
    if(lastCenter) {x.value+=center.x-lastCenter.x; y.value+=center.y-lastCenter.y}
    lastDistance=distance;lastCenter=center
  } else if(scale.value>1) { x.value+=next.x-old.x; y.value+=next.y-old.y }
}
function up(e) { pointers.delete(e.pointerId);lastDistance=0;lastCenter=null }
function selected(square) { if(!moved) emit('select',square) }
window.addEventListener('pointerup',up)
onBeforeUnmount(()=>window.removeEventListener('pointerup',up))
</script>
<template>
  <div class="board-viewport" :class="{compact}">
    <div class="board-controls"><button @click="zoom(scale-.25)" aria-label="Zoom out">−</button><button @click="reset">{{ game.t('reset') }}</button><button @click="zoom(scale+.25)" aria-label="Zoom in">+</button><button v-if="compact" @click="$emit('expand')">⤢</button></div>
    <div class="board-clip" @wheel.prevent="zoom(scale + ($event.deltaY < 0 ? .15 : -.15))" @pointerdown="down" @pointermove="move" @pointercancel="up">
      <div class="board-transform" :style="{transform:`translate(${x}px,${y}px) scale(${scale})`}"><Board2D @select="selected" /></div>
    </div>
  </div>
</template>
