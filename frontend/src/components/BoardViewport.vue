<script setup>
import { ref, watch, onBeforeUnmount } from 'vue'
import Board2D from './Board2D.vue'
import { useGame } from '../stores/game'
const props = defineProps({ compact:Boolean, ownershipToggle:Boolean, fit:Boolean })
const emit = defineEmits(['select','expand','decks'])
const game = useGame(), scale = ref(1), x = ref(0), y = ref(0)
const showOwners = ref(false)
const viewport = ref(null), controls = ref(null), fitWidth = ref(null)
let fitObserver
function fitBoard() {
  if (!props.fit || !viewport.value || !controls.value) return
  if (!window.matchMedia('(min-width:901px)').matches) { fitWidth.value = null; return }
  const parent = viewport.value.parentElement
  const style = getComputedStyle(viewport.value)
  const horizontalPadding = parseFloat(style.paddingLeft) + parseFloat(style.paddingRight)
  const verticalPadding = parseFloat(style.paddingTop) + parseFloat(style.paddingBottom)
  const sidebar = style.display === 'grid'
  const controlsSpace = sidebar ? controls.value.offsetWidth + (parseFloat(style.columnGap) || 0) : 0
  const headerSpace = sidebar ? 0 : controls.value.offsetHeight + (parseFloat(getComputedStyle(controls.value).marginBottom) || 0)
  const boardSize = Math.min(parent.clientWidth - horizontalPadding - controlsSpace, parent.clientHeight - verticalPadding - headerSpace)
  fitWidth.value = Math.max(0, Math.floor(boardSize + horizontalPadding + controlsSpace))
}
watch([viewport, controls, () => props.fit], () => {
  fitObserver?.disconnect()
  if (!props.fit || !viewport.value || !controls.value) return
  fitObserver = new ResizeObserver(fitBoard)
  fitObserver.observe(viewport.value.parentElement)
  fitObserver.observe(controls.value)
  fitBoard()
})
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
function showDecks() { if(!moved) emit('decks') }
window.addEventListener('pointerup',up)
onBeforeUnmount(()=>{ window.removeEventListener('pointerup',up); fitObserver?.disconnect() })
</script>
<template>
  <div ref="viewport" class="board-viewport" :class="{compact}" :style="{width:fitWidth == null ? undefined : `${fitWidth}px`}">
    <div ref="controls" class="board-controls"><label v-if="ownershipToggle" class="owner-toggle"><input v-model="showOwners" type="checkbox">{{ game.t('showOwners') }}</label><div class="zoom-controls"><button @click="zoom(scale-.25)" :aria-label="game.t('zoomOut')">−</button><button @click="reset">{{ game.t('reset') }}</button><button @click="zoom(scale+.25)" :aria-label="game.t('zoomIn')">+</button><button v-if="compact" @click="$emit('expand')" :aria-label="game.t('zoom')">⤢</button></div></div>
    <div class="board-clip" @wheel.prevent="zoom(scale + ($event.deltaY < 0 ? .15 : -.15))" @pointerdown="down" @pointermove="move" @pointercancel="up">
      <div class="board-transform" :style="{position:'absolute',width:`${scale*100}%`,height:`${scale*100}%`,left:`calc(50% + ${x}px)`,top:`calc(50% + ${y}px)`,transform:'translate(-50%,-50%)'}"><Board2D :show-owners="showOwners" @select="selected" @decks="showDecks" /></div>
    </div>
  </div>
</template>
<style scoped>
.board-controls{flex-wrap:wrap;align-items:center}.owner-toggle{display:flex;align-items:center;gap:8px;margin:0 auto 0 0;padding:6px 9px;background:#e5ecdf;color:#17382b;border-radius:6px;font-size:13px;cursor:pointer}.owner-toggle input{width:16px;height:16px;margin:0}
.zoom-controls{display:flex;align-items:center;gap:5px}
@media(min-width:901px){
  .board-viewport:not(.compact){display:grid;grid-template-columns:minmax(0,1fr) 160px;column-gap:12px;align-items:start}
  .board-viewport:not(.compact) .board-clip{grid-column:1;grid-row:1;min-width:0}
  .board-viewport:not(.compact) .board-controls{grid-column:2;grid-row:1;width:160px;display:flex;flex-direction:column;align-items:stretch;gap:12px;margin:0}
  .board-viewport:not(.compact) .owner-toggle{margin:0;font-size:11px}
  .board-viewport:not(.compact) .zoom-controls{justify-content:center}
}
</style>
