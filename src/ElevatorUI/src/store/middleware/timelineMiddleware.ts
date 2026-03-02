/* eslint-disable @typescript-eslint/no-explicit-any */
import type { Middleware } from "@reduxjs/toolkit";
import { snapshotCaptured } from "../slices/timelineSlice";

// Skip recording these — they're either noisy bookkeeping or would cause recursion
const SKIP = new Set([
  "timeline/snapshotCaptured",
  "timeline/recordingStarted",
  "timeline/recordingStopped",
  "timeline/replayEntered",
  "timeline/replayExited",
  "timeline/playStarted",
  "timeline/playStopped",
  "timeline/cursorSet",
  "timeline/timelineCleared",
  "timeline/replaySnapshot",
  "connection/wsConnecting",
  "connection/wsConnected",
  "connection/wsDisconnected",
  "connection/wsReconnecting",
  "connection/wsConnect",
  "connection/wsDisconnect",
]);

export const timelineMiddleware: Middleware<Record<string, never>, any, any> =
  (storeAPI) => (next) => (action: any) => {
    // Pass the action through first so we snapshot the resulting state
    const result = next(action);

    const state = storeAPI.getState();
    if (!state.timeline?.isRecording) return result;
    if (SKIP.has(action?.type)) return result;

    storeAPI.dispatch(
      snapshotCaptured({
        type: action.type ?? "unknown",
        snapshot: {
          elevator: state.elevator,
          connection: state.connection,
          passengers: state.passengers,
        },
      })
    );

    return result;
  };
