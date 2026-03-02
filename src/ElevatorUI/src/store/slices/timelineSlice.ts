import { createAction, createSlice } from "@reduxjs/toolkit";
import type { PayloadAction } from "@reduxjs/toolkit";
import type { SystemStatusDto, Passenger, ReturnTripRequest } from "@/types/elevator";
import type { RequestLogEntry } from "./passengersSlice";
import type { WsStatus } from "./connectionSlice";

// --- Snapshot type: full app state minus timeline itself ---

export interface StateSnapshot {
  elevator: { status: SystemStatusDto | null; messageCount: number; vipFloors: number[] };
  connection: { isConnected: boolean; wsStatus: WsStatus; backoffMs: number };
  passengers: { passengers: Passenger[]; nextId: number; returnQueue: ReturnTripRequest[]; requestLog: RequestLogEntry[] };
}

export interface RecordedAction {
  id: number;
  timestamp: number;
  type: string;
  snapshot: StateSnapshot;
}

// Dispatched by the component to restore a past snapshot.
// Handled by the wrapped root reducer in store/index.ts (not by this slice).
export const replaySnapshot = createAction<StateSnapshot>("timeline/replaySnapshot");

// --- State ---

interface TimelineState {
  isRecording: boolean;
  isReplaying: boolean;
  isPlaying: boolean;
  actions: RecordedAction[];
  cursor: number;
  nextId: number;
}

const MAX_SNAPSHOTS = 300;

const initialState: TimelineState = {
  isRecording: false,
  isReplaying: false,
  isPlaying: false,
  actions: [],
  cursor: -1,
  nextId: 0,
};

// --- Slice ---

const timelineSlice = createSlice({
  name: "timeline",
  initialState,
  reducers: {
    recordingStarted(state) {
      state.isRecording = true;
      state.isReplaying = false;
      state.isPlaying = false;
      state.actions = [];
      state.cursor = -1;
      state.nextId = 0;
    },
    recordingStopped(state) {
      state.isRecording = false;
      state.isPlaying = false;
    },
    snapshotCaptured(
      state,
      action: PayloadAction<{ type: string; snapshot: StateSnapshot }>
    ) {
      if (!state.isRecording) return;
      if (state.actions.length >= MAX_SNAPSHOTS) state.actions.shift();
      state.actions.push({
        id: state.nextId++,
        timestamp: Date.now(),
        type: action.payload.type,
        snapshot: action.payload.snapshot,
      });
      // Keep cursor at tip when not in manual replay
      if (!state.isReplaying) {
        state.cursor = state.actions.length - 1;
      }
    },
    replayEntered(state, action: PayloadAction<number>) {
      state.isReplaying = true;
      state.isRecording = false;
      state.cursor = action.payload;
    },
    replayExited(state) {
      state.isReplaying = false;
      state.isPlaying = false;
    },
    playStarted(state) {
      state.isPlaying = true;
    },
    playStopped(state) {
      state.isPlaying = false;
    },
    cursorSet(state, action: PayloadAction<number>) {
      state.cursor = Math.max(0, Math.min(state.actions.length - 1, action.payload));
    },
    timelineCleared(state) {
      state.isRecording = false;
      state.isReplaying = false;
      state.isPlaying = false;
      state.actions = [];
      state.cursor = -1;
    },
  },
});

export const {
  recordingStarted,
  recordingStopped,
  snapshotCaptured,
  replayEntered,
  replayExited,
  playStarted,
  playStopped,
  cursorSet,
  timelineCleared,
} = timelineSlice.actions;

export default timelineSlice.reducer;
export { initialState as timelineInitialState };

// --- Selectors ---

type TL = { timeline: TimelineState };

export const selectIsRecording = (s: TL) => s.timeline.isRecording;
export const selectIsReplaying = (s: TL) => s.timeline.isReplaying;
export const selectIsPlaying = (s: TL) => s.timeline.isPlaying;
export const selectRecordedActions = (s: TL) => s.timeline.actions;
export const selectCursor = (s: TL) => s.timeline.cursor;
export const selectCurrentRecordedAction = (s: TL) => {
  const { actions, cursor } = s.timeline;
  return cursor >= 0 && cursor < actions.length ? actions[cursor] : null;
};
