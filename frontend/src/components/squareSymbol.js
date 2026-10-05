export function squareSymbol(square) {
  return ({ go:'←', jail:'▦', parking:'🚗', goToJail:'👮', chance:'?', chest:'🧰', rail:'🚂', tax:'◇', utility:square.id === 12 ? '💡' : '🚰' })[square.type]
}
