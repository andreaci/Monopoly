import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import { HubConnectionBuilder, HttpTransportType, LogLevel } from '@microsoft/signalr'
import { translate } from '../locales'
import { translateError } from '../locales/errors'

export const useGame = defineStore('game', () => {
  const state = ref(null), meId = ref(null), manager = ref(false), online = ref(false), error = ref(''), busy = ref(false), clock = ref(Date.now()), actions = ref([])
  let hub, clockTimer, retryTimer, stopped = false
  let matchId = new URLSearchParams(location.search).get('match')
  const url = path => `${import.meta.env.BASE_URL}${path}${matchId ? '?match='+encodeURIComponent(matchId) : ''}`
  const me = computed(() => state.value?.players.find(p => p.id === meId.value))
  const active = computed(() => state.value?.players.find(p => p.id === state.value.activePlayerId))
  const language = ref(null)
  const t = key => translate(language.value ?? state.value?.settings.language, key)
  const money = amount => `${state.value?.settings.currency ?? '$'}${new Intl.NumberFormat(state.value?.settings.italian ? 'it-IT' : 'en-US').format(amount ?? 0)}`
  const can = type => online.value && !busy.value && actions.value.includes(type)
  const player = id => state.value?.players.find(p => p.id === id)
  const square = id => state.value?.board.find(s => s.id === id)
  const deed = id => state.value?.deeds.find(d => d.squareId === id)
  const now = computed(() => clock.value)
  let offset = 0
  function apply(snapshot, personal = false) {
    if (state.value && state.value.matchId === snapshot.matchId && snapshot.revision < state.value.revision) return
    offset = Date.parse(snapshot.serverTime) - Date.now()
    state.value = snapshot
    if (personal) actions.value = snapshot.allowedActions
  }
  async function request(path, body) {
    const response = await fetch(url(`api/${path}`), { credentials:'same-origin', ...(body !== undefined ? { method:'POST', headers:{'Content-Type':'application/json'}, body:JSON.stringify(body) } : {}) })
    if (!response.ok) { const data = await response.json().catch(() => ({})); throw new Error(translateError(language.value ?? state.value?.settings.language, data.error ?? `HTTP ${response.status}`)) }
    return response.status === 204 ? null : response.text().then(text => text ? JSON.parse(text) : null)
  }
  async function refresh() { if (hub?.state === 'Connected') apply(await hub.invoke('GetState'), true) }
  async function connect() {
    stopped = false
    const session = await request('session')
    matchId = session.state.matchId
    const address = new URL(location.href); address.searchParams.set('match',matchId); history.replaceState(history.state,'',address)
    meId.value = session.playerId; manager.value = session.manager; apply(session.state, true)
    hub = new HubConnectionBuilder().withUrl(url('hubs/game'), { transport:HttpTransportType.WebSockets, skipNegotiation:true }).withAutomaticReconnect([0,1000,3000,5000,10000]).configureLogging(LogLevel.Error).build()
    hub.on('state', snapshot => { apply(snapshot); actions.value = []; refresh().catch(e => error.value = e.message) })
    hub.onreconnecting(() => { online.value = false; actions.value = [] })
    hub.onreconnected(async () => { online.value = true; const s = await request('session'); meId.value = s.playerId; manager.value = s.manager; await refresh() })
    hub.onclose(() => { online.value = false; if (!stopped) retryTimer = setTimeout(() => connect().catch(e => error.value = e.message), 3000) })
    try { await hub.start(); online.value = true; await refresh() }
    catch (e) { online.value = false; error.value = e.message; if (!stopped) retryTimer = setTimeout(() => connect().catch(e => error.value = e.message), 3000) }
    if (!clockTimer) clockTimer = setInterval(() => clock.value = Date.now() + offset, 100)
  }
  async function reconnectIdentity() { stopped = true; clearTimeout(retryTimer); await hub?.stop(); await connect() }
  async function run(operation) {
    if (busy.value) return false
    busy.value = true; error.value = ''
    try { await operation(); return true } catch(e) { error.value = translateError(language.value ?? state.value?.settings.language, e.message.replace('An unexpected error occurred invoking \'SendCommand\' on the server. HubException: ', '')); return false } finally { busy.value = false }
  }
  async function command(type, data = {}) { return run(async () => { await hub.invoke('SendCommand', { id:crypto.randomUUID?.() ?? `${Date.now()}-${Math.random()}`, type, ...data }); await refresh() }) }
  return { state, language, url, meId, manager, me, active, online, error, busy, now, actions, t, money, can, player, square, deed, connect, reconnectIdentity, request, run, command }
})
