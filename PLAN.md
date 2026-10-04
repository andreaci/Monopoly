# Monopoly implementation plan

## Scope

Build a local multiplayer Monopoly application for 2–6 players. A small ASP.NET Core backend serves the Vue frontend and owns all match state in memory. Players join from phones over the same local network. No database, accounts, or match persistence. Restarting the backend ends the match.

Version 1 uses a detailed 2D board. Version 2 adds an optional 3D board using the same state and rules.

## Agreed requirements

- Vue frontend written in JavaScript, using Vite, Vue Router, and Pinia.
- C# backend with SignalR over WebSockets for commands and live updates.
- One match per server instance initially.
- Manager selects settings, displays the QR code, and starts the match. After starting, the PC is a display with board viewing controls; gameplay actions belong to phones.
- Players choose a name and an available token when joining. Match size is 2–6.
- Identity is a server-issued opaque cookie; refreshing or reconnecting restores the existing player while the match is alive.
- Wait for a disconnected player when their turn arrives. Required outstanding decisions also wait for their player.
- PC layout: large board on the left, player names/balances/cards on the right, and bank-owned cards across the bottom. The reference sketch's colors identify regions, not required UI colors.
- Phones show a small board, balance, cards grouped by color, and contextual action controls. Tapping the board opens an enlarged view.
- Board supports zoom, pan, and reset on PC and phones, including pinch gestures on phones.
- Property deeds use the supplied classic cream card style, with color bands, borders, rents, building costs, and mortgage details. Railway and utility deeds have their own symbols. Compact previews open a readable enlarged card.
- The server generates both dice. The PC animates them over the board; phones show a full-screen animation identifying the current player. Every screen ends on the same result.
- Transactions, card draws, and auctions are visible on the PC.
- English uses the supplied US board. Italian uses the supplied Italian street names.
- Language options: English, Italian (€), and Italian (£). Further languages can be added through data files.
- Modern profiles default to 1,500 starting cash and 200 for passing GO/VIA. Italian £ defaults to 150,000 and 20,000, with the other monetary values scaled consistently by 100.
- Starting cash and GO/VIA payment can be customized in setup. These overrides do not automatically change purchase prices, rents, or card amounts.
- Setup checkboxes: auctions, trading, houses/hotels, jail, and mortgages. Bankruptcy is always enabled.
- Unowned streets can be purchased directly, without auction.
- When a player lands on another player's street, they may offer to buy it. The owner can refuse; acceptance starts an auction in which anyone can bid.
- Rent is settled after the auction, to the resulting owner, unless the visitor becomes the owner.
- Disabling jail still moves a player sent to jail to that square, without detention.
- Use the same complete Chance/Imprevisti and Community Chest/Probabilità effects across profiles, translating text and scaling amounts.

## Proposed gameplay defaults

These fill gaps in the agreed requirements and should be kept explicit during implementation.

- Enable all optional rules by default; settings are fixed once the match starts.
- Assign turn order by lobby joining order; all tokens start on GO/VIA.
- Buying is available for all unowned purchasable spaces, including railways and utilities. Declining leaves the deed in the bank.
- The special landing-triggered auction applies to colored streets. Railways and utilities may be transferred through trading.
- The visitor chooses whether to propose a purchase before rent is paid. If no offer is made or the owner refuses, rent goes to the existing owner.
- Auction bidders include the owner and visitor. The owner sets the opening price before accepting. Bids are limited to available cash; an owner victory keeps ownership without a payment to themselves.
- Use a visible server-controlled countdown, extended by a valid late bid. No bids means no sale. Freeze the countdown if an eligible bidder disconnects and resume on reconnection.
- After a sale, transfer cash and ownership atomically, then calculate rent using the resulting ownership. Mortgaged streets charge no rent. Buildings must be sold before a street can be auctioned or traded.
- Trading requires explicit agreement from both parties and supports cash, eligible deeds, and held Get Out of Jail Free cards. Resolve trades atomically.
- Restrict building, selling buildings, mortgaging, and proposing trades to the active player's management phase or required debt settlement. Recipients can respond when prompted.
- Follow standard rules for doubles, three doubles, enabled jail, color groups, railway/utility rent, even building, the finite house/hotel supply, mortgages, and bankruptcy. Record the chosen mortgage-transfer and liquidation rules explicitly.
- With buildings disabled, undeveloped rent rules still apply, including the full-color-group rent multiplier.
- Shuffle each deck on the server and recycle cards. Held Get Out of Jail Free cards stay outside the deck until used or returned.
- Play continues until one solvent player remains. No round limit.
- A player reconnecting during an animation receives current state and catches up; animation completion does not depend on browser acknowledgments.

## Architecture

### Backend

- `Domain`: board definitions, money profiles, player/deed/deck state, commands, events, and turn phases.
- `GameEngine`: validates commands and applies rules independently of HTTP, WebSockets, and visual rendering.
- `MatchService`: owns the in-memory match, serializes mutations, manages timers and connection presence, and broadcasts updates.
- `GameHub`: accepts authenticated player commands and publishes snapshots/events through SignalR using WebSockets.
- HTTP endpoints: setup, join, session restoration, and initial snapshots. Joining issues an HttpOnly cookie that also identifies the WebSocket connection; clients cannot select their identity by sending a player ID.
- Manager setup/start authorization is separate from player sessions. Player sessions cannot invoke manager commands. After starting, manager gameplay controls do not exist.
- Every accepted mutation increments a match revision. Commands carry unique IDs to avoid duplicate rolls, bids, and payments after retries.
- Clients receive authoritative snapshots and ordered presentation events. Shuffle order and session credentials are not exposed.
- Timers and rule transitions run on the server. Only the active player can roll, and no second roll is accepted while a previous roll is resolving.

### Frontend

- Views: manager setup/lobby, PC match display, phone join/lobby, phone match, and match result.
- Shared components: `Board2D`, `BoardViewport`, `Token`, `PropertyCard`, `CardDetails`, `DiceOverlay`, `AuctionPanel`, and `TransactionFeed`.
- Pinia holds the server snapshot, connection status, and temporary UI selections. Game rules and money calculations stay on the server.
- Contextual phone controls are driven by server-provided allowed actions, with server validation on every command.
- Board rendering uses HTML/CSS and SVG details so labels remain readable when zoomed. Match the supplied square positions, rotations, corner geometry, property bands, and center deck areas.
- Dice events include player, result, animation start time, and duration, so displays follow a shared timeline without using animations as game authority.

### Data

- Stable square and card IDs are independent of display language.
- Board definitions store square order, type, color group, prices, rent tables, building costs, and mortgage values.
- Localization files store UI text, street names, and translated card text. Currency profiles store symbols and amount scaling.
- Freeze a documented modern set of 16 Chance and 16 Community Chest cards. Verify every effect and translation against authoritative edition references before implementing the decks; board photos alone do not specify the decks.
- Maintain a reference checklist for all 40 squares and every deed. Images supplied in this chat guide presentation; transcribe their names into project data.

## Turn flow

1. Wait for the active player's connection, then allow pre-roll management and rolling.
2. Generate dice on the server, broadcast the presentation event, and lock further rolls.
3. Move the token and resolve passing GO/VIA, square effects, and any chained card movement.
4. Await purchases, owner consent, auction bids, rent, or other required choices as applicable.
5. If payment cannot be completed, enter debt settlement: sell buildings, mortgage eligible deeds, or make permitted trades; declare bankruptcy if settlement fails.
6. Allow post-roll management, then continue for doubles or end the turn. Apply jail rules and detect a winner.

## Implementation phases

### 1. Foundation and lobby

Create the frontend/backend structure, static hosting, build script, WebSocket connection, manager settings, LAN address selection, QR code, phone joining, token selection, and cookie-based restoration. Reuse the architectural patterns from `E:\Repos\SeguiLaFolla`, adapting its local-storage identity approach to cookies. QR links must use a reachable PC LAN address rather than localhost.

Deliverable: 2–6 phones can join, settings lock at start, and refresh restores the correct player.

### 2. Data and main screens

Define the board, profiles, and deeds. Build the detailed 2D board and viewport, PC layout, mobile inventory, card inspection, and language switching during setup.

Deliverable: both supplied boards and all bank/player deeds render correctly, with working zoom and pan.

### 3. Core game engine

Implement turn phases, dice, synchronized presentation, movement, purchases, ownership, rents, taxes, doubles, and jail variants. Display transactions and active-player status.

Deliverable: players can take authoritative turns from their phones and buy/pay on all applicable spaces.

### 4. Complete card decks

Implement all 32 cards, deck handling, movement chains, repairs, payments between players, and held jail cards. Translate text and apply currency profiles.

Deliverable: every card has a defined, executable effect in all three profiles.

### 5. Property management and auctions

Implement owner-approved landing auctions, bidding/timers, rent after auction, trading, mortgages, even building, finite supplies, and checkbox enforcement.

Deliverable: every agreed optional rule works from phones and is displayed on the PC.

### 6. Debt, bankruptcy, and reconnection

Complete liquidation, creditor/bank transfers, elimination, winner detection, mid-decision restoration, duplicate command handling, and disconnected-player waiting.

Deliverable: a full match can finish, and reconnection restores any outstanding decision without duplicating transactions.

### 7. Packaging and acceptance

Provide a PowerShell build/publish script that puts the Vue build into `published/wwwroot` beside the .NET 10 backend, plus LAN startup instructions. Provide a multi-stage Dockerfile and GitHub Actions container build. Validate the game engine's meaningful rules and manually run a local multi-client match.

Acceptance scenarios: 2- and 6-player lobbies; all currency profiles; zoom and card inspection; each setup toggle; card movement chains; refused/accepted/no-bid auctions; visitor wins versus third-party wins and rent afterward; insufficient funds; mortgages/buildings; bankruptcy; refresh and disconnect during turns/auctions; duplicate commands; synchronized dice outcomes; and reset after server restart.

### Version 2: 3D

Add a 3D renderer as a separate board component with camera/zoom controls and shared selection events. Preserve the version 1 engine, identity, phone actions, and snapshots. Keep the 2D view available.

## Suggested repository structure

```text
backend/Monopoly.Server/
  Domain/
  GameEngine/
  Services/
  Hubs/
  Endpoints/
  Data/
frontend/
  src/
    api/
    stores/
    views/
    components/board/
    components/cards/
    components/game/
    locales/
    styles/
tests/Monopoly.GameEngine.Tests/
build.ps1
README.md
Dockerfile
.github/workflows/docker.yml
PLAN.md
```

Keep the engine readable for a school exercise: explicit states, small rule handlers, and a documented command/event contract.
# Updates to the original plan

The implemented version now supports independent simultaneous matches, each with 2–6 players, and internet access through a reverse proxy. QR links use the page's loaded origin and include the match ID. Hosting at `/monopoly/` is optional. English US currency is dollars; Italian profiles retain euros and classic pounds. All interface labels, rule descriptions and player-facing errors are localized. Phone card draws use animated fullscreen reveals, and players can choose metal or painted wooden token styles. The requested Monopoly Inline font is bundled locally. These updates supersede the local-only, single-match assumptions in the original plan below.
