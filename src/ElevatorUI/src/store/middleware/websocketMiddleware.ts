import type { Middleware } from "@reduxjs/toolkit";
import type { SystemStatusDto } from "@/types/elevator";
import { statusReceived } from "../slices/elevatorSlice";
import {
  wsConnect,
  wsDisconnect,
  wsConnecting,
  wsConnected,
  wsDisconnected,
  wsReconnecting,
} from "../slices/connectionSlice";
import {
  passengersSync,
  selectPassengers,
  scheduleReturnTrip,
} from "../slices/passengersSlice";

const WS_URL =
  (process.env.NEXT_PUBLIC_WS_URL ?? "ws://localhost:5081") + "/ws";
const MAX_BACKOFF_MS = 10000;

// Use loose typing to avoid circular dep with store/index.ts
/* eslint-disable @typescript-eslint/no-explicit-any */
export const websocketMiddleware: Middleware<Record<string, never>, any, any> =
  (storeAPI) => {
  let ws: WebSocket | null = null;
  let backoffMs = 1000;
  let reconnectTimer: ReturnType<typeof setTimeout> | null = null;
  let active = false;

  function connect() {
    if (!active) return;

    storeAPI.dispatch(wsConnecting());

    ws = new WebSocket(WS_URL);

    ws.onopen = () => {
      if (!active) return;
      backoffMs = 1000;
      storeAPI.dispatch(wsConnected());
    };

    ws.onmessage = (event) => {
      if (!active) return;
      try {
        const data: SystemStatusDto = JSON.parse(event.data as string);

        // 1. Update elevator status
        storeAPI.dispatch(statusReceived(data));

        // 2. Snapshot passengers before sync (to detect transitions)
        const prevPassengers = selectPassengers(storeAPI.getState());
        const prevRidingIds = new Set(
          prevPassengers
            .filter((p) => p.status === "riding")
            .map((p) => p.id)
        );

        // 3. Sync passenger states
        storeAPI.dispatch(passengersSync(data.elevators));

        // 4. Find newly arrived passengers (riding → arrived)
        const nextPassengers = selectPassengers(storeAPI.getState());
        const newlyArrived = nextPassengers.filter(
          (p) =>
            p.status === "arrived" &&
            prevRidingIds.has(p.id) &&
            p.returnDelaySec !== undefined &&
            p.returnDelaySec > 0 &&
            p.destinationFloor !== 1
        );

        // 5. Schedule return trips for newly arrived passengers
        for (const passenger of newlyArrived) {
          storeAPI.dispatch(scheduleReturnTrip(passenger));
        }
      } catch {
        // ignore malformed messages
      }
    };

    ws.onclose = () => {
      if (!active) return;
      ws = null;
      const delay = backoffMs;
      backoffMs = Math.min(delay * 2, MAX_BACKOFF_MS);
      storeAPI.dispatch(wsDisconnected({ backoffMs: delay }));
      storeAPI.dispatch(wsReconnecting());
      reconnectTimer = setTimeout(connect, delay);
    };

    ws.onerror = () => {
      ws?.close();
    };
  }

  function disconnect() {
    active = false;
    if (reconnectTimer !== null) {
      clearTimeout(reconnectTimer);
      reconnectTimer = null;
    }
    if (ws) {
      ws.close();
      ws = null;
    }
  }

  return (next) => (action) => {
    if (wsConnect.match(action)) {
      active = true;
      connect();
      return;
    }

    if (wsDisconnect.match(action)) {
      disconnect();
      storeAPI.dispatch(wsDisconnected({ backoffMs }));
      return;
    }

    return next(action);
  };
};
