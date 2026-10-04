# Monopoly — play with your friends

Vue 3 / JavaScript frontend, ASP.NET Core **.NET 10** backend, and SignalR over WebSockets. Multiple independent in-memory matches, each for 2–6 players. No database or accounts. Restarting the server clears all matches and player sessions.

## Build and run on Windows

Prerequisites: Node.js 24 with npm, .NET 10 SDK to build, and the ASP.NET Core 10 runtime to run the published application.

```powershell
.\build.ps1
cd published
dotnet Monopoly.Server.dll
```

The script builds Vue, publishes C#, and copies the frontend into `published/wwwroot`. It uses `npm ci` with the committed lockfile. Options: `-SkipNpmInstall`, `-OutputDir <path>` and `-BasePath /monopoly/`. The latter builds frontend URLs and writes matching backend configuration to `published/appsettings.json`.

For this workspace's initial verification, ASP.NET Core 10 was installed into the ignored `.dotnet-local` directory because the system had the .NET 10 SDK but lacked that ASP.NET runtime. You can also run this built copy from the repository root with:

```powershell
.\.dotnet-local\dotnet.exe .\published\Monopoly.Server.dll --contentRoot "$PWD\published"
```

Use a normal system installation of the ASP.NET Core 10 runtime for ongoing development or deployment.

Open **http://localhost:5080** on the manager's PC. Choose the language, amounts, and optional rules. The QR uses the address where the page was opened and includes `?match=<match-id>`. Open the manager page using an address your players can reach (a LAN address or public proxy URL). Players scan it, choose a name/token, and wait in the lobby. Start when 2–6 players are connected.

The PC becomes the main display. Players roll, buy, trade, bid, manage properties, and end turns from `/play` on their phones. `/display` is an additional viewing route. The app and its assets need no Internet connection while playing. Windows may need an inbound firewall allowance for TCP 5080 on the local network.

Cookie identification restores the same player after refresh or reconnection. A disconnected player's required turn/decision waits, and auctions pause while an eligible bidder is disconnected. Reconnection requires the original browser and cookies. Multiple tabs for one player count as one identity; closing the last tab marks the player disconnected.

## Docker

```powershell
docker build -t monopoly .
docker run --name monopoly --rm -p 5080:5080 monopoly
```

The multi-stage Dockerfile compiles Vue and .NET 10, then runs only the ASP.NET runtime and built static frontend. It runs as the image's non-root app user. The backend serves the frontend on port 5080 under **`/monopoly/` by default**. Open `http://localhost:5080/monopoly/`. To build for root hosting, use `docker build --build-arg APP_BASE_PATH=/ -t monopoly .`.

In Docker, enter the **manager access code printed in the container logs** on the setup page. A container does not see the host browser as a loopback connection. Only the manager session can change settings or start the game. The code is newly generated each server start.

Open the manager page using the intended player-facing URL, for example `https://games.example.com/monopoly/`. The QR derives its origin, port and base path from that page; it never lists host interfaces.

## GitHub Actions

### Optional subfolder and reverse proxy

```powershell
.\build.ps1 -BasePath /monopoly/
docker build --build-arg APP_BASE_PATH=/monopoly/ -t monopoly .
```

Keep `/monopoly/` in the upstream request path, enable WebSocket upgrades, and preserve the public Host header. Configure `TRUSTED_PROXIES` with a comma-separated list of proxy IPs when forwarding `X-Forwarded-Proto`, `X-Forwarded-Host` and `X-Forwarded-For`; loopback proxies are trusted by default. Set `ALLOW_LOCAL_MANAGER=false` for public proxy deployments so setup always requires the manager code. TLS can terminate at the proxy. Frontend and backend base paths must agree. Windows publishing defaults to root hosting; Docker defaults to `/monopoly/`.

Each new table gets an opaque match ID. Its API requests, WebSocket group and player/manager cookies are isolated from other tables. Refreshing a match URL rejoins that table; **New table** creates a separate match while existing tables continue running. Keep the full match link when opening `/play` or `/display`.

Phone card draws appear as animated fullscreen cards with a Continue button. Consecutive draws are queued, and dismissed cards are remembered for the browser tab. Tokens include both metal-style figurines and painted wooden pieces based on the supplied references.

The locally bundled font is the **Monopoly Inline demo** from the requested [Monopoly Sans source](https://www.dafontfree.io/monopoly-sans-font/), which labels it personal use only. See `frontend/public/fonts/README.md`.

[`.github/workflows/docker.yml`](.github/workflows/docker.yml) runs only when a version tag in the form `vX.X.X` is pushed, such as `v1.2.3` or `v10.20.30`. It runs the engine checks, builds the full Docker image with Buildx caching using the default `/monopoly/` base path, starts the image, verifies both the API and frontend routes, and uploads the image archive as an artifact. No registry credentials are required.

Download and unzip the `monopoly-container` artifact, then:

```powershell
docker load -i monopoly-image.tar
docker run --rm -p 5080:5080 monopoly:ci
```

## Development

```powershell
# Terminal 1
dotnet run --project backend/Monopoly.Server

# Terminal 2
cd frontend
npm ci
npm run dev
```

Vite listens on the LAN and proxies `/api` and `/hubs` to port 5080. Open the frontend through the intended player-facing address on port **5173** during development; the QR uses that same origin. Production serves the frontend and API from one origin.

## Implemented rules

- Server-authoritative turns, cryptographic dice/shuffling, duplicate command IDs, money transfers, 40 squares, and all 28 deeds.
- English US uses $1,500 / $200 defaults; Italian euro uses €1,500 / €200; Italian classic pound uses £150,000 / £20,000. Deeds, taxes, repairs, and card amounts scale by 100 in the classic profile. Setup overrides affect starting cash and GO/VIA only.
- Auctions, trading, buildings, jail, and mortgages can be enabled independently. Bankruptcy is always enabled. Settings lock on start. Joining order determines turns.
- Unowned property can be purchased directly or left in the bank. There is no bank auction when a purchase is declined.
- Landing on another player's undeveloped colored street allows a purchase proposal. The owner may refuse. Acceptance opens a 25-second auction for everyone, including owner and visitor. The owner sets its opening price. Late bids extend to at least 8 seconds. Bids cannot exceed cash.
- A winning bid transfers cash and the deed together. If the owner wins or nobody bids, ownership stays unchanged. The visitor then pays rent to the resulting owner unless they own it. Developed groups must have all buildings sold before auction or trade. Railways/utilities are traded rather than landing-auctioned. Mortgaged deeds charge no rent.
- Trading requires consent, can include cash/deeds/jail cards, and locks the initiating player's other actions until answered or canceled. Recipients of mortgaged deeds pay 10% of the mortgage principal immediately and retain the mortgage; redemption later pays principal plus 10% again.
- Four houses before a hotel, even construction/sale, a bank supply of 32 houses/12 hotels, and half-price building sales. Selling an entire color group's buildings is available when hotel downgrades cannot obtain four bank houses.
- Three doubles send the player to jail. Enabled jail permits a paid exit, a held card, or doubles. Three failed attempts require payment before moving. With jail disabled, being sent there moves the token to the jail square without detention.
- Debt settlement permits selling buildings, mortgages if enabled, and trades. Bankruptcy is offered after no building sale or available mortgage remains. Assets transfer to the creditor, with mortgage interest; a bank creditor returns deeds to the bank instead of auctioning them. The last remaining player wins.
- Property management and initiating trades occur in the active player's pre-roll/end phase or the owing player's debt phase. Turn-dependent controls are validated again on the server.

These auction and bank-return rules intentionally follow this project's agreed custom game rules. The remaining rule baseline is the [Hasbro classic rulebook](https://www.hasbro.com/common/instruct/Monopoly_Vintage.pdf).

## Card set and localization

`Data/Cards.cs` defines a frozen classic US set: **16 Chance and 16 Community Chest effects**, using post-2008 monetary values. Captions are concise original English/Italian summaries rather than scans or verbatim card artwork. This is the familiar classic set, not the later community-themed Community Chest revision. One set of effects drives all three profiles; street references use stable square IDs.

Decks shuffle on the server. Normal cards return to the bottom. Get Out of Jail Free cards remain out until used or returned. Card movement, nearest-railway double rent, nearest-utility fresh dice, repairs, and player-to-player payments are implemented.

To add a language, add its UI dictionary in `frontend/src/locales`, names/captions in backend data, and its setup money profile. The renderer uses the same square IDs across profiles.

## Verification

The dependency-free C# scenario runner exits nonzero on failure:

```powershell
dotnet run --project tests/Monopoly.GameEngine.Tests
```

The existing 60 scenarios cover lobby validation, turn authority, replay protection, jail, purchases, auction outcomes and disconnects, building supply, mortgages, trades, debt/bankruptcy, currency scaling, the full decks, every card effect, and an automated match through to a winner. Browser checks also cover two real WebSocket sessions, phone purchase controls, refreshed cookie identity, Italian pound settings, zoom and deed details, and synchronized PC dice presentation. The Docker build is checked by CI; it was not run locally because Docker is not installed on this PC.

Version 2's 3D board is deferred as planned. Version 1 renders the board with HTML/CSS and card symbols, preserving readable labels when zoomed; it does not use scanned proprietary artwork.
