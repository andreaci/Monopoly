<script setup>
import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import QRCode from 'qrcode'
import { useGame } from './stores/game'
import { tokenNames } from './locales'
import BoardViewport from './components/BoardViewport.vue'
import PropertyCard from './components/PropertyCard.vue'
import DiceOverlay from './components/DiceOverlay.vue'
import TurnPrompt from './components/TurnPrompt.vue'
import PropertyDecision from './components/PropertyDecision.vue'
import CardReveal from './components/CardReveal.vue'
import TokenIcon from './components/TokenIcon.vue'
import MoneyEffects from './components/MoneyEffects.vue'
import PositionCard from './components/PositionCard.vue'
import ReconnectStatus from './components/ReconnectStatus.vue'
import StatisticsOverlay from './components/StatisticsOverlay.vue'
import SpecialSquareDetails from './components/SpecialSquareDetails.vue'
import SquareOccupants from './components/SquareOccupants.vue'
import DeckGuide from './components/DeckGuide.vue'

const game = useGame(), route = useRoute(), router = useRouter()
const tableElement = ref(null), tableHeight = ref(null)
let tableObserver
function fitTable() {
  if (tableElement.value) tableHeight.value = Math.max(0, Math.floor(window.innerHeight - tableElement.value.getBoundingClientRect().top - 16))
}
watch(tableElement, element => {
  tableObserver?.disconnect()
  if (!element) return
  tableObserver = new ResizeObserver(fitTable)
  tableObserver.observe(element.parentElement)
  fitTable()
})
watch(() => [game.state?.waitingFor, game.state?.phase, game.error], async () => { await nextTick(); fitTable() })
onMounted(() => window.addEventListener('resize', fitTable))
onBeforeUnmount(() => { tableObserver?.disconnect(); window.removeEventListener('resize', fitTable) })
const baseUrl = import.meta.env.BASE_URL
const form = ref({language:'en',startingCash:1500,goPayment:200,auctions:true,trading:true,buildings:true,jail:true,mortgages:true,bankRent:false,buildingsOnlyWhenPresent:false,tradingOnlyWhenOccupied:false})
const joinUrl = ref(''), qr = ref(''), copied = ref(false)
const joinName = ref(''), joinToken = ref(''), selected = ref(null), expanded = ref(false), selectedDeed = ref(''), bidAmount = ref(1), opening = ref(1), tradeOpen = ref(false)
const tradeDraft = ref({ to:'',offerCash:0,requestCash:0,offerDeeds:[],requestDeeds:[],offerJailCards:0,requestJailCards:0 })
const statisticsOpen = ref(false), decksOpen = ref(false)
const phone = computed(()=>route.path === '/join' || route.path === '/play')
const owned = computed(()=>game.state?.deeds.filter(d=>d.ownerId === game.meId).map(d=>game.square(d.squareId)) ?? [])
const manageOptions = computed(()=>!game.state?.settings.buildingsOnlyWhenPresent ? owned.value : owned.value.filter(square=>square.id===game.me?.position))
const groups = computed(()=>owned.value.reduce((result,s)=>{ (result[s.group || s.type] ??= []).push(s); return result },{}))
const bankCards = computed(()=>game.state?.deeds.filter(d=>!d.ownerId).map(d=>game.square(d.squareId)) ?? [])
const waitingPlayer = computed(()=>game.player(game.state?.waitingFor))
const phaseText = computed(()=>game.t(({rent:'rentPhase',consent:'consentPhase',auction:'auctionPhase',debt:'debtPhase',card:'cardDraw',utilityArrival:'newLocation'})[game.state?.phase] ?? game.state?.phase))
const auctionSeconds = computed(()=>game.state?.auction?.paused ? Math.ceil(game.state.auction.remainingSeconds) : Math.max(0,Math.ceil((Date.parse(game.state?.auction?.endsAt)-game.now)/1000)))
const startReady = computed(()=>game.state?.players.length >= 2 && game.state.players.every(p=>p.connected) && game.online && !game.busy)
const manageable = computed(()=>['build','sellBuilding','sellGroup','mortgage','unmortgage'].some(game.can))
const occupiedForTrade = square=>!game.state?.settings.tradingOnlyWhenOccupied || game.state.players.some(player=>!player.bankrupt&&player.position===square.id)
const offeredDeeds = computed(()=>owned.value.filter(occupiedForTrade))
const receiverDeeds = computed(()=>game.state?.deeds.filter(d=>d.ownerId===tradeDraft.value.to).map(d=>game.square(d.squareId)).filter(occupiedForTrade) ?? [])
const frozen = computed(()=>game.state?.waitingFor || !game.online)
let loadingSettings = true

watch(joinUrl, async value=> { try { const url = new URL(value); url.searchParams.set('match',game.state.matchId); if (url.href !== value) { joinUrl.value=url.href; return } qr.value = ['http:','https:'].includes(url.protocol) ? await QRCode.toDataURL(url.href,{width:280,margin:2,color:{dark:'#17382b',light:'#ffffff'}}) : '' } catch { qr.value='' } })
watch(()=>form.value.language, (value,old)=>{
  if (!phone.value && game.manager) game.language = value
  if(value === old || loadingSettings) return
  const nextScale=value === 'it-GBP' ? 100:1, previousScale=old === 'it-GBP' ? 100:1
  form.value.startingCash=Math.round(form.value.startingCash/previousScale*nextScale)
  form.value.goPayment=Math.round(form.value.goPayment/previousScale*nextScale)
})
watch(()=>game.state?.auction?.highestBid, ()=>bidAmount.value = Math.max(game.state?.auction?.openingBid ?? 1,(game.state?.auction?.highestBid ?? 0)+game.state.settings.scale))
watch(()=>game.state?.phase, ()=>{ tradeOpen.value=false; selectedDeed.value='' })
watch(()=>game.me, value=>{ if(value && route.path==='/join') router.replace({path:'/play',query:{match:game.state.matchId}}) })
watch(()=>game.state?.settings.language, language=>{ game.language=null; document.documentElement.lang = language?.startsWith('it') ? 'it':'en' })
watch(()=>game.language ?? game.state?.settings.language, language=>{ document.documentElement.lang=language?.startsWith('it') ? 'it':'en'; document.title=`Monopoly · ${game.t('table')}` })

onMounted(async ()=>{
  try {
    await game.connect()
    form.value = { ...game.state.settings }
    await new Promise(resolve=>queueMicrotask(resolve))
    loadingSettings = false
    joinUrl.value = `${location.origin}${baseUrl}join?match=${game.state.matchId}`
    if(game.me && route.path==='/join') router.replace({path:'/play',query:{match:game.state.matchId}})
  } catch(e) { game.error=e.message }
})
async function save(start=false) {
  await game.run(async ()=>{ await game.request('settings',form.value); if(start) await game.request('start',{}); })
}
async function join() {
  await game.run(async ()=>{ const result=await game.request('join',{name:joinName.value,token:joinToken.value}); game.meId=result.playerId; await game.reconnectIdentity(); router.replace({path:'/play',query:{match:game.state.matchId}}) })
}
async function copy() {
  try { await navigator.clipboard.writeText(joinUrl.value); copied.value=true; setTimeout(()=>copied.value=false,2000) } catch { game.error=joinUrl.value }
}
function choose(square) { if(phone.value && !expanded.value) { expanded.value=true; return } selected.value=square }
async function propertyAction(type) { if(selectedDeed.value!=='') await game.command(type,{squareId:Number(selectedDeed.value)}) }
async function sendTrade() { if(await game.command('tradeOffer',{trade:{...tradeDraft.value}})) tradeOpen.value=false }
function openTrade() { tradeDraft.value={to:'',offerCash:0,requestCash:0,offerDeeds:[],requestDeeds:[],offerJailCards:0,requestJailCards:0}; tradeOpen.value=true }
async function bankrupt() { if(window.confirm(game.t('bankruptConfirm'))) await game.command('bankrupt') }
</script>

<template>
  <div v-if="!game.state" class="loading-screen"><div class="wordmark">MONOPOLY</div><p>{{ game.t('loading') }}</p><p v-if="game.error" role="alert">{{ game.error }}</p><a v-if="game.error" :href="baseUrl">{{ game.t('newMatch') }}</a></div>
  <main v-else :class="{'phone-app':phone}" class="app-shell">
    <header class="app-header"><RouterLink :to="{path:phone ? '/play':'/',query:{match:game.state.matchId}}" class="wordmark">MONOPOLY</RouterLink><span class="header-subtitle">{{ game.t('table') }}</span><button v-if="game.state.phase!=='lobby'" class="statistics-button" :title="game.t('statistics')" :aria-label="game.t('statistics')" @click="statisticsOpen=true">📊</button><a v-if="!phone && game.manager" :href="baseUrl" class="new-match">{{ game.t('newMatch') }}</a><span class="connection-dot" :class="{online:game.online}"></span><span class="connection-label">{{ game.online ? game.t('connected'):game.t('offline') }}</span></header>
    <div v-if="game.error" class="error-banner" role="alert"><span>{{ game.error }}</span><button @click="game.error=''" :aria-label="game.t('close')">×</button></div>
    <div v-if="!game.online" class="notice" role="status">{{ game.t('offline') }}</div>

    <section v-if="game.state.phase==='lobby' && !phone" class="setup-layout">
      <div class="panel setup-panel"><span class="eyebrow">{{ game.t('localMultiplayer') }}</span><h1>{{ game.t('setup') }}</h1><p class="muted">{{ game.t('welcome') }}</p>
        <template v-if="game.manager">
          <label>{{ game.t('language') }}<select v-model="form.language"><option value="en">English · US · $</option><option value="it-EUR">Italiano · €</option><option value="it-GBP">Italiano · £</option></select></label>
          <div class="form-row"><label>{{ game.t('startingCash') }}<input v-model.number="form.startingCash" type="number" min="1" max="100000000"></label><label>{{ game.t('goPayment') }}<input v-model.number="form.goPayment" type="number" min="0" max="1000000"></label></div>
          <h3>{{ game.t('rules') }}</h3><div class="rule-options"><label v-for="key in ['auctions','trading','buildings','jail','mortgages','bankRent','buildingsOnlyWhenPresent','tradingOnlyWhenOccupied']" :key="key" class="checkbox"><input v-model="form[key]" type="checkbox"><span><strong>{{ game.t(key) }}</strong><small class="rule-description">{{ game.t(key+'Help') }}</small></span></label></div>
          <p class="fineprint">{{ game.t('rulesHint') }}</p><div class="button-row"><button :disabled="game.busy || !game.online" @click="save()">{{ game.t('save') }}</button><button class="primary" :disabled="!startReady" @click="save(true)">{{ game.t('start') }} →</button></div><p class="fineprint">{{ game.t('allReady') }}</p>
        </template>
        <template v-else><p>{{ game.t('managedTable') }}</p><a :href="baseUrl">{{ game.t('newMatch') }}</a></template>
      </div>
      <div class="lobby-column"><div class="panel qr-panel"><span class="eyebrow">{{ game.t('join') }}</span><img v-if="qr" :src="qr" tabindex="0" :alt="game.t('qrCode')" class="qr-image"><label>{{ game.t('lan') }}<input :value="joinUrl" type="url" readonly></label><button :disabled="!qr" @click="copy">{{ game.t(copied?'copied':'copy') }}</button></div>
        <div class="panel"><h3>{{ game.t('players') }} <span class="count">{{ game.state.players.length }}/6</span></h3><div v-for="p in game.state.players" :key="p.id" class="lobby-player"><span class="token-avatar"><TokenIcon :token="p.token" /></span><strong>{{ p.name }} <ReconnectStatus :player="p" /></strong><span class="status" :class="{present:p.connected}">{{ game.t(p.connected?'connected':'disconnected') }}</span></div><p v-if="!game.state.players.length" class="muted">{{ game.t('waiting') }}…</p></div>
      </div>
    </section>

    <section v-else-if="phone && !game.me" class="panel join-panel"><span class="eyebrow">MONOPOLY · {{ game.state.players.length }}/6</span><h1>{{ game.t('join') }}</h1><template v-if="game.state.phase==='lobby' && game.state.players.length<6"><form @submit.prevent="join"><label>{{ game.t('name') }}<input v-model="joinName" required maxlength="24" autocomplete="nickname" :placeholder="game.t('name')"></label><label>{{ game.t('chooseToken') }}</label><div v-for="style in ['metal','wood']" :key="style"><h3>{{ game.t(style+'Tokens') }}</h3><div class="token-picker"><button v-for="token in game.state.tokens.filter(t=>t.startsWith('wood-')===(style==='wood'))" :key="token" type="button" :class="{selected:joinToken===token}" :disabled="game.state.players.some(p=>p.token===token)" @click="joinToken=token"><span><TokenIcon :token="token" /></span><small>{{ tokenNames[game.state.settings.italian?'it':'en'][token] }}</small></button></div></div><button type="submit" class="primary wide" :disabled="!joinName.trim() || !joinToken || game.busy || !game.online">{{ game.t('join') }} →</button></form></template><p v-else>{{ game.t('full') }}</p></section>

    <section v-else-if="phone && game.state.phase==='lobby'" class="panel phone-lobby"><div class="big-token"><TokenIcon :token="game.me.token" /></div><h1>{{ game.me.name }}</h1><p>{{ game.t('lobbyHint') }}</p><div v-for="p in game.state.players" :key="p.id" class="lobby-player"><span><TokenIcon :token="p.token" /></span><strong>{{ p.name }} <ReconnectStatus :player="p" /></strong><span class="status" :class="{present:p.connected}">{{ game.t(p.connected?'connected':'disconnected') }}</span></div></section>

    <template v-else>
      <div v-if="game.state.phase==='finished'" class="winner-banner"><span>🏆</span><h1>{{ game.player(game.state.winnerId)?.name }} {{ game.t('winner') }}</h1><p>{{ game.t('winnerHint') }}</p></div>

      <div v-if="phone" class="turn-strip"><span class="token-avatar"><TokenIcon :token="game.active?.token" /></span><div><span class="eyebrow">{{ game.active?.id===game.meId ? game.t('yourTurn'):game.t('turn') }}</span><strong>{{ game.active?.name }} <ReconnectStatus :player="game.active" /></strong></div><span v-if="waitingPlayer && waitingPlayer.id!==game.active?.id" class="waiting-player-name">{{ waitingPlayer.name }} <ReconnectStatus :player="waitingPlayer" /></span><span class="phase-pill">{{ phaseText }}</span></div>

      <div v-if="!phone" ref="tableElement" class="desktop-table" :style="{'--table-height':tableHeight == null ? undefined : `${tableHeight}px`}">
        <section class="main-board"><BoardViewport @decks="decksOpen=true" fit :ownership-toggle="game.manager" @select="choose" /><DiceOverlay /></section>
        <aside class="panel players-panel"><h2>{{ game.t('players') }}</h2><section v-for="(p,index) in game.state.players" :key="p.id" class="player-section" :class="{current:p.id===game.state.activePlayerId,eliminated:p.bankrupt}" :style="{'--player-color':['#d95b58','#458fc4','#d8ad43','#9072b8','#479d81','#d776a1'][index]}"><div class="player-heading"><span><TokenIcon :token="p.token" /></span><strong>{{ p.name }} <ReconnectStatus :player="p" /></strong><span v-if="p.id===game.state.activePlayerId" class="current-turn-badge">{{ game.t('activePlayer') }}</span><b>{{ game.money(p.cash) }}</b><PositionCard :player="p" @select="selected=$event" /></div><small v-if="p.inJail || p.bankrupt">{{ game.t(p.bankrupt?'eliminated':p.inJail?'jailStatus':'disconnected') }}</small><div class="mini-cards"><button v-for="d in game.state.deeds.filter(d=>d.ownerId===p.id)" :key="d.squareId" class="card-button" @click="selected=game.square(d.squareId)"><PropertyCard :square="game.square(d.squareId)" /></button><span v-for="id in p.jailCards" :key="id" class="held-card">▦</span></div></section></aside>
        <section class="panel bank-panel"><div class="bank-heading"><h2>{{ game.t('bank') }}</h2><span>{{ game.t('supply') }}: {{ game.state.housesLeft }} ⌂ · {{ game.state.hotelsLeft }} 🏨</span></div><div class="bank-cards"><button v-for="square in bankCards" :key="square.id" class="card-button" @click="selected=square"><PropertyCard :square="square" /></button></div></section>
      </div>

      <div v-else class="phone-game"><div class="phone-balance"><div><span class="eyebrow">{{ game.me.name }} <ReconnectStatus :player="game.me" /></span><h1>{{ game.money(game.me.cash) }}</h1></div><span class="big-token"><TokenIcon :token="game.me.token" /></span><PositionCard :player="game.me" @select="selected=$event" /></div>
        <section class="inventory"><h2>{{ game.t('inventory') }}</h2><p v-if="!owned.length" class="muted">{{ game.t('noCards') }}</p><div v-for="(cards,group) in groups" :key="group" class="property-group"><span class="group-color" :style="{background:cards[0].color || '#aaa'}"></span><div class="phone-cards"><button v-for="square in cards" :key="square.id" class="card-button" @click="selected=square"><PropertyCard :square="square" /></button></div></div><button v-for="id in game.me.jailCards" :key="id" class="held-card wide" @click="selected={card:game.state.cards.find(c=>c.id===id)}">▦ {{ game.t('jailCards') }}</button></section>
        <BoardViewport @decks="decksOpen=true" compact @select="choose" @expand="expanded=true" /><p class="fineprint">{{ game.t('zoom') }} · {{ game.t('boardHelp') }}</p>
        <div v-if="game.me.bankrupt" class="notice">{{ game.t('eliminated') }}</div>
        <section class="panel action-panel" v-if="!game.me.bankrupt && game.state.phase!=='finished'">
          <h3 v-if="game.active?.id===game.meId">{{ game.square(game.me.position)?.name }}</h3>
          <div class="action-buttons"><button v-for="action in ['roll','jailPay','jailCard','endTurn']" :key="action" v-show="game.actions.includes(action)" :class="{primary:['roll','buy','endTurn'].includes(action)}" :disabled="!game.can(action)" @click="game.command(action)">{{ game.t(action) }} <span v-if="action === 'payRent'">{{ game.money(game.state.rentDue) }}</span><span v-if="action==='buy'">{{ game.money(game.square(game.me.position).price) }}</span></button></div>
          <p v-if="game.can('requestAuction')" class="fineprint">{{ game.t('auctionHint') }}</p>
          <div v-if="game.state.phase==='debt' && game.state.debt?.from===game.meId" class="debt-box"><h3>{{ game.t('debt') }} · {{ game.money(game.state.debt.amount) }}</h3><p>{{ game.t('debtHint') }}</p><button v-if="game.actions.includes('bankrupt')" class="danger" :disabled="!game.can('bankrupt')" @click="bankrupt">{{ game.t('bankrupt') }}</button></div>
          <div v-if="manageable" class="management"><h3>{{ game.t('manage') }}</h3><select v-model="selectedDeed"><option value="">{{ game.t('selectProperty') }}</option><option v-for="square in manageOptions" :key="square.id" :value="square.id">{{ square.name }}{{ game.deed(square.id).mortgaged ? ' · '+game.t('mortgaged'):'' }}</option></select><div class="button-row"><button v-for="action in ['build','sellBuilding','sellGroup','mortgage','unmortgage']" :key="action" v-show="game.actions.includes(action)" :disabled="!game.can(action) || selectedDeed===''" @click="propertyAction(action)">{{ game.t(action) }}</button></div></div>
          <button v-if="game.actions.includes('tradeOffer')" :disabled="!game.can('tradeOffer')" @click="openTrade">{{ game.t('tradeOffer') }}</button><button v-if="game.actions.includes('tradeCancel')" :disabled="!game.can('tradeCancel')" @click="game.command('tradeCancel')">{{ game.t('tradeCancel') }}</button>
        </section>
      </div>

      <section v-if="game.state.auction" class="panel auction-panel" :class="{'auction-live':game.state.phase==='auction'}"><span class="eyebrow">{{ game.t('auction') }}</span><h2>{{ game.square(game.state.auction.squareId)?.name }}</h2><p>{{ game.player(game.state.auction.visitorId)?.name }} → {{ game.player(game.state.auction.ownerId)?.name }}</p><template v-if="game.state.phase==='consent'"><p>{{ game.t('consent') }}</p><div v-if="phone && game.actions.includes('auctionConsent')"><label>{{ game.t('opening') }}<input v-model.number="opening" type="number" min="1"></label><div class="button-row"><button class="primary" :disabled="!game.can('auctionConsent')" @click="game.command('auctionConsent',{accept:true,amount:opening})">{{ game.t('acceptAuction') }}</button><button :disabled="!game.can('auctionConsent')" @click="game.command('auctionConsent',{accept:false})">{{ game.t('refuse') }}</button></div></div></template><template v-else><div class="auction-numbers"><strong>{{ game.money(game.state.auction.highestBid || game.state.auction.openingBid) }}</strong><span>{{ auctionSeconds }} {{ game.t('countdown') }}</span></div><p>{{ game.state.auction.bidderId ? game.player(game.state.auction.bidderId)?.name : game.t('noBids') }}</p><p v-if="game.state.auction.paused" class="notice">{{ game.t('auctionPaused') }}</p><form v-if="phone && !game.me?.bankrupt" @submit.prevent="game.command('bid',{amount:bidAmount})"><label>{{ game.t('amount') }}<input v-model.number="bidAmount" type="number" :min="Math.max(game.state.auction.openingBid,game.state.auction.highestBid+1)" :max="game.me.cash"></label><button class="primary" :disabled="!game.can('bid')">{{ game.t('bid') }}</button></form></template></section>

      <section v-if="game.state.trade" class="panel trade-summary"><span class="eyebrow">{{ game.t('tradeReview') }}</span><h3>{{ game.player(game.state.trade.from)?.name }} ↔ {{ game.player(game.state.trade.to)?.name }}</h3><div class="trade-columns"><div><b>{{ game.player(game.state.trade.from)?.name }}</b><p>{{ game.money(game.state.trade.offerCash) }}</p><p v-for="id in game.state.trade.offerDeeds" :key="id">{{ game.square(id).name }}</p><p v-if="game.state.trade.offerJailCards">{{ game.state.trade.offerJailCards }} · {{ game.t('jailCards') }}</p></div><div><b>{{ game.player(game.state.trade.to)?.name }}</b><p>{{ game.money(game.state.trade.requestCash) }}</p><p v-for="id in game.state.trade.requestDeeds" :key="id">{{ game.square(id).name }}</p><p v-if="game.state.trade.requestJailCards">{{ game.state.trade.requestJailCards }} · {{ game.t('jailCards') }}</p></div></div><p class="fineprint">{{ game.t('transferInterest') }}</p><div v-if="phone && game.actions.includes('tradeRespond')" class="button-row"><button class="primary" :disabled="!game.can('tradeRespond')" @click="game.command('tradeRespond',{accept:true})">{{ game.t('accept') }}</button><button :disabled="!game.can('tradeRespond')" @click="game.command('tradeRespond',{accept:false})">{{ game.t('refuse') }}</button></div></section>
      <section v-if="game.state.lastCard" class="panel drawn-card"><span class="eyebrow">{{ game.t(game.state.lastCard.deck==='chance'?'chance':'chest') }}</span><p>{{ game.state.lastCard.text }}</p></section>
      <section class="panel log-panel"><h3>{{ game.t('log') }}</h3><ol><li v-for="entry in [...game.state.log].reverse().slice(0,phone?8:20)" :key="entry.sequence"><time>{{ new Date(entry.at).toLocaleTimeString([], {hour:'2-digit',minute:'2-digit'}) }}</time><span>{{ entry.text }}</span></li></ol></section>
    </template>

    <DiceOverlay v-if="phone" phone /><CardReveal :display="!phone" /><TurnPrompt v-if="phone" /><PropertyDecision v-if="phone" /><MoneyEffects />
    <div v-if="expanded" class="modal board-modal" @click.self="expanded=false"><section class="modal-content"><header><h2>{{ game.t('zoom') }}</h2><button @click="expanded=false">{{ game.t('close') }} ×</button></header><BoardViewport @decks="decksOpen=true" @select="square=>selected=square" /><p class="fineprint">{{ game.t('boardHelp') }}</p></section></div>
    <DeckGuide v-if="decksOpen" @close="decksOpen=false" />
    <StatisticsOverlay v-if="statisticsOpen" @close="statisticsOpen=false" />
    <div v-if="selected" class="modal" @click.self="selected=null"><section class="modal-content card-modal"><button class="modal-close" @click="selected=null" :aria-label="game.t('close')">×</button><PropertyCard v-if="selected.price && ['street','rail','utility'].includes(selected.type)" :square="selected" detailed /><SpecialSquareDetails v-else-if="selected.type" :square="selected" /><template v-else><h2>{{ game.t(selected.card?.deck==='chance'?'chance':'chest') }}</h2><p>{{ selected.card?.text }}</p></template><SquareOccupants v-if="selected.type" :square="selected" /></section></div>
    <div v-if="tradeOpen" class="modal" @click.self="tradeOpen=false"><section class="modal-content trade-modal"><header><h2>{{ game.t('tradeOffer') }}</h2><button @click="tradeOpen=false">×</button></header><form @submit.prevent="sendTrade"><label>{{ game.t('tradeTo') }}<select v-model="tradeDraft.to" required @change="tradeDraft.requestDeeds=[];tradeDraft.requestJailCards=0"><option value="">—</option><option v-for="p in game.state.players.filter(p=>p.id!==game.meId&&!p.bankrupt)" :key="p.id" :value="p.id">{{ p.name }}</option></select></label><div class="form-row"><label>{{ game.t('offerCash') }}<input v-model.number="tradeDraft.offerCash" type="number" min="0" :max="game.me.cash"></label><label>{{ game.t('requestCash') }}<input v-model.number="tradeDraft.requestCash" type="number" min="0" :max="game.player(tradeDraft.to)?.cash ?? 0"></label></div><div class="trade-columns"><fieldset><legend>{{ game.t('offerDeeds') }}</legend><label v-for="square in offeredDeeds" :key="square.id" class="checkbox"><input v-model="tradeDraft.offerDeeds" type="checkbox" :value="square.id">{{ square.name }}</label></fieldset><fieldset><legend>{{ game.t('requestDeeds') }}</legend><label v-for="square in receiverDeeds" :key="square.id" class="checkbox"><input v-model="tradeDraft.requestDeeds" type="checkbox" :value="square.id">{{ square.name }}</label></fieldset></div><div class="form-row"><label>{{ game.t('offerJail') }}<input v-model.number="tradeDraft.offerJailCards" type="number" min="0" :max="game.me.jailCards.length"></label><label>{{ game.t('requestJail') }}<input v-model.number="tradeDraft.requestJailCards" type="number" min="0" :max="game.player(tradeDraft.to)?.jailCards.length ?? 0"></label></div><p class="fineprint">{{ game.t('transferInterest') }}</p><button class="primary wide" :disabled="!game.can('tradeOffer') || !tradeDraft.to">{{ game.t('confirm') }}</button></form></section></div>
  </main>
</template>
