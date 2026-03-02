# Elevator Control System

A full-stack multi-elevator control system built with **.NET 8** (backend) and **Next.js 15 + Redux** (frontend), featuring real-time WebSocket communication, intelligent dispatch algorithms, passenger tracking, and live system reconfiguration.

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                   ElevatorUI  (Next.js 15)                      │
│                   http://localhost:3000                          │
│                                                                  │
│  React Components ←→ Redux Store (elevatorSlice, passengersSlice│
│       ↕                    ↕                                     │
│  useElevatorApi()     websocketMiddleware                        │
│  (REST calls)         (WS + auto-reconnect)                      │
└────────────────┬───────────────────────┬────────────────────────┘
                 │  HTTP REST            │  WebSocket
                 │  POST /api/requests   │  ws://localhost:5081/ws
                 │  PUT  /api/config     │  (500 ms broadcast)
                 ▼                       ▼
┌─────────────────────────────────────────────────────────────────┐
│                  ElevatorAPI  (.NET 8 Minimal API)              │
│                  http://localhost:5081                           │
│                                                                  │
│  REST Endpoints ─→ ElevatorSystemHolder ─→ ElevatorSystem       │
│  WebSocketBroadcastService (500 ms)                              │
│  SystemRunnerService (dispatcher + per-elevator workers)         │
│  Swagger UI: /swagger                                            │
└─────────────────────────────────────────────────────────────────┘
                            │
┌───────────────────────────▼─────────────────────────────────────┐
│                  ElevatorSystem  (.NET 8 Library)                │
│                                                                  │
│  Centralized dispatcher · Thread-safe state · 134 tests          │
│  Algorithms: Simple · SCAN · LOOK · Custom                       │
│  Elevator types: Local · Express · Freight                       │
└─────────────────────────────────────────────────────────────────┘
```

---

## Features

- **7-elevator default setup**: 4 Local · 2 Express · 1 Freight
- **Floors 1–20** with VIP floor 13 restricted to VIP access
- **Express elevators**: serve Lobby + a configured upper zone only
- **4 dispatch algorithms**: Simple, SCAN, LOOK, Custom
- **Live reconfiguration**: change floor range, timings, elevators without restart
- **Passenger tracking**: client-side simulation of waiting → riding → arrived → returning
- **Return trips**: passengers automatically return to lobby after a configurable delay
- **Maintenance mode**: take individual elevators out of service
- **Emergency stop**: system-wide halt with one click
- **Performance metrics**: wait times, ride times, utilization, floor heatmap
- **Per-elevator log download** from the UI
- **Time-travel debug** timeline (replay up to 300 state snapshots)
- **134 unit and integration tests**

---

## Getting Started

### Prerequisites

| Tool | Version |
|------|---------|
| .NET SDK | 8 or higher |
| Node.js | 18 or higher |
| npm | 9 or higher |

### Clone & Build

```bash
git clone git@github.com:fvcoelho/elevator-sys.git
cd elevator-sys
dotnet build
dotnet test
```

---

## Running the System

Open **three terminal windows**:

```bash
# Terminal 1 — Core elevator engine (optional, runs inside ElevatorAPI)
dotnet run --project src/ElevatorSystem/ElevatorSystem.csproj

# Terminal 2 — REST API + WebSocket server
dotnet run --project src/ElevatorAPI/ElevatorAPI.csproj
# → http://localhost:5081  |  Swagger: http://localhost:5081/swagger

# Terminal 3 — Next.js UI
cd src/ElevatorUI
npm install
npm run dev
# → http://localhost:3000
```

> The API and UI are the primary components. The console app (`ElevatorSystem`) is optional for direct CLI interaction.

---

## Default Configuration

| Label | Type    | Initial Floor | Served Floors |
|-------|---------|--------------|---------------|
| A     | Local   | 1            | All           |
| B     | Local   | 7            | All           |
| C     | Local   | 14           | All           |
| D     | Local   | 20           | All           |
| E1    | Express | 1            | 1, 11–15      |
| E2    | Express | 1            | 1, 16–20      |
| F1    | Freight | 1            | All           |

**VIP floor: 13** — marked with red dashed border in the UI; requires VIP access level.

Timing defaults: 500 ms door open · 500 ms per floor · 500 ms door transition.

---

## REST API Reference

### Requests

| Method | Path | Description |
|--------|------|-------------|
| `POST` | `/api/requests` | Submit a ride request |

```json
// POST /api/requests
{
  "pickupFloor": 1,
  "destinationFloor": 13,
  "priority": "Normal",        // "Normal" | "High"
  "accessLevel": "Standard",   // "Standard" | "VIP"
  "preferredElevatorType": null // "Local" | "Express" | "Freight" | null
}
```

### Status & Elevators

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/api/status` | Full system snapshot |
| `GET` | `/api/elevators` | List all elevators |
| `GET` | `/api/elevators/{index}` | Single elevator state |

### Control

| Method | Path | Description |
|--------|------|-------------|
| `PUT` | `/api/dispatch/algorithm` | Change algorithm (`Simple`/`SCAN`/`LOOK`/`Custom`) |
| `POST` | `/api/emergency/stop` | Emergency stop all elevators |
| `POST` | `/api/emergency/resume` | Resume from emergency stop |
| `POST` | `/api/elevators/{index}/maintenance` | Toggle maintenance mode |

### Configuration

| Method | Path | Description |
|--------|------|-------------|
| `PUT` | `/api/config` | Hot-swap full system configuration |
| `POST` | `/api/elevators` | Add an elevator at runtime |

### Metrics & Logs

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/api/metrics` | Performance analytics |
| `GET` | `/api/logs/{label}` | Download elevator log file |

### WebSocket

Connect to `ws://localhost:5081/ws`. The server broadcasts a `SystemStatusDto` JSON payload every **500 ms** to all connected clients.

---

## UI Components

| Component | Description |
|-----------|-------------|
| `elevator-shaft` | Per-elevator card: floors, state, passenger names, VIP/target highlights, maintenance toggle |
| `building-view` | Floor-by-floor passenger view; VIP floors marked red dashed; lobby wraps multi-line |
| `elevator-panel` | Request ride form: name, destination, return delay, priority |
| `system-config` | Live reconfiguration: floors, timings, algorithm, VIP floors, add/remove elevators with express zone picker |
| `status-bar` | Connection badge, message counter, emergency stop, algorithm selector, metrics popup |
| `request-log` | Scrollable log of all passenger requests with requestId |
| `traffic-generator` | Batch-generate random passenger traffic |
| `ws-payload-viewer` | Debug panel showing raw WebSocket JSON |
| `dev-timeline` | Time-travel: record and scrub up to 300 state snapshots |

---

## Redux Store

```
store/
├── slices/
│   ├── elevatorSlice      — SystemStatusDto from WebSocket; VIP floor list
│   ├── connectionSlice    — WebSocket lifecycle (connecting/connected/reconnecting…)
│   ├── passengersSlice    — Client-side passenger simulation + request log
│   └── timelineSlice      — Dev tool: state snapshot history
├── middleware/
│   ├── websocketMiddleware — Connects ws://localhost:5081/ws, dispatches status,
│   │                         syncs passenger boarding/arrival, schedules return trips
│   └── timelineMiddleware  — Captures snapshots when recording is active
└── subscribers/
    └── localStorageSubscriber — Persists key slices across page reloads
```

### Passenger Lifecycle

```
passengerAdded (waiting)
    → websocketMiddleware detects elevator at pickup floor with matching requestId
    → status: riding  (elevatorIndex assigned)
    → elevator reaches destination floor
    → status: arrived
    → scheduleReturnTrip() fires after returnDelaySec
    → status: returning  →  new API request back to lobby  →  waiting again
```

---

## Dispatch Algorithms

| Algorithm | Strategy |
|-----------|----------|
| **Simple** | Idle-preference + closest distance (O(n)) |
| **SCAN** | Sweep direction; +100 bonus for same-direction requests |
| **LOOK** | Like SCAN but reverses at the last pending request |
| **Custom** | Dynamic queue reordering for optimal multi-stop routes |

High-priority requests bypass idle preference and compete with all elevators.

---

## Project Structure

```
elevator-sys/
├── src/
│   ├── ElevatorSystem/          # Core engine (.NET 8 library/console)
│   │   ├── Elevator.cs          # Thread-safe elevator logic
│   │   ├── ElevatorSystem.cs    # Dispatcher + request queue
│   │   ├── Request.cs           # Immutable ride request model
│   │   ├── ElevatorConfig.cs    # Type config (Local/Express/Freight)
│   │   ├── DispatchAlgorithm.cs # Simple, SCAN, LOOK, Custom
│   │   ├── FloorAccess.cs       # VIP / floor restriction rules
│   │   ├── PerformanceTracker.cs# Analytics & metrics
│   │   └── Program.cs           # Interactive CLI
│   │
│   ├── ElevatorAPI/             # REST API + WebSocket server (.NET 8)
│   │   ├── Endpoints/           # Mapped endpoint groups
│   │   ├── Models/              # DTOs (request, status, config, metrics)
│   │   ├── Services/            # WebSocketBroadcastService, SystemRunnerService,
│   │   │                        #   ElevatorSystemHolder, ElevatorSystemFactory
│   │   ├── Configuration/       # Options classes
│   │   └── appsettings.json     # Default system config
│   │
│   └── ElevatorUI/              # Next.js 15 frontend
│       └── src/
│           ├── app/             # Next.js app router (page.tsx, layout.tsx)
│           ├── components/      # React components (UI + shadcn)
│           ├── store/           # Redux Toolkit store, slices, middleware
│           ├── hooks/           # useElevatorApi, useAppSelector, useAppDispatch
│           ├── types/           # TypeScript interfaces for all DTOs
│           └── lib/             # Utilities, passenger name generator
│
├── tests/
│   └── ElevatorSystem.Tests/    # 134 unit + integration tests (xUnit)
├── docs/                        # Specs and implementation plans
└── ElevatorSystem.sln
```

---

## Thread Safety

| Resource | Mechanism |
|----------|-----------|
| Request queue | `ConcurrentQueue<Request>` (lock-free) |
| Request IDs | `Interlocked.Increment` |
| Dispatch scoring | `_dispatchLock` critical section |
| Elevator floor / state | Lock-protected properties |
| Elevator target floors | `ConcurrentQueue<int>` (lock-free) |
| WebSocket connections | `ConcurrentDictionary` |

---

## License

Demonstration project for educational purposes.
