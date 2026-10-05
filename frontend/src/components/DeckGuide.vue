<script setup>
import { onMounted, ref } from 'vue'
import { useGame } from '../stores/game'
defineEmits(['close'])
const game = useGame()
const closeButton = ref(null)
onMounted(() => closeButton.value?.focus())
</script>

<template>
  <div class="modal deck-guide-backdrop" @click.self="$emit('close')" @keydown.esc="$emit('close')">
    <section class="modal-content deck-guide" role="dialog" aria-modal="true" :aria-label="game.t('deckGuide')">
      <button ref="closeButton" class="modal-close" @click="$emit('close')" :aria-label="game.t('close')">×</button>
      <h2>{{ game.t('deckGuide') }}</h2>
      <div class="deck-guide-grid">
        <article v-for="deck in ['chance','chest']" :key="deck">
          <div class="deck-picture" :class="deck"><span aria-hidden="true">{{ deck === 'chance' ? '?' : '🧰' }}</span><strong>{{ game.t(deck) }}</strong></div>
          <p>{{ game.t(`${deck}DeckDescription`) }}</p>
          <p class="draw-explanation">{{ game.t(`${deck}DeckWhen`) }}</p>
        </article>
      </div>
    </section>
  </div>
</template>

<style scoped>
.deck-guide-backdrop{z-index:2000}.deck-guide{max-width:660px;padding-top:48px;text-align:center}
.deck-guide h2{font-size:21px;margin-bottom:30px}
.deck-guide-grid{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:28px}
.deck-picture{width:90%;min-height:150px;margin:0 auto 24px;display:flex;flex-direction:column;align-items:center;justify-content:center;gap:10px;border:1px solid #879a83;box-shadow:3px 3px #becbb6,5px 5px #71836d;text-transform:uppercase;letter-spacing:1px}
.deck-picture.chance{background:#e4a462;transform:rotate(4deg)}.deck-picture.chest{background:#aed9e4;transform:rotate(-4deg)}
.deck-picture>span{font-size:64px;line-height:1}.chance>span{color:white;font-weight:700}.deck-picture strong{font-size:14px}
.deck-guide p{font-size:14px;line-height:1.6}.draw-explanation{padding:12px;border-radius:10px;background:#e5ebdf;font-weight:600;margin-bottom:0}
@media(max-width:480px){.deck-guide-grid{grid-template-columns:1fr;gap:30px}.deck-picture{max-width:240px}}
</style>
