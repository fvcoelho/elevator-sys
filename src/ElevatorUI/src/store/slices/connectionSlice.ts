import { createAction, createSlice } from "@reduxjs/toolkit";
import type { PayloadAction } from "@reduxjs/toolkit";

// --- Actions intercepted by middleware (not reducers) ---

export const wsConnect = createAction("connection/wsConnect");
export const wsDisconnect = createAction("connection/wsDisconnect");

// --- State ---

export type WsStatus =
  | "idle"
  | "connecting"
  | "connected"
  | "reconnecting"
  | "disconnected";

interface ConnectionState {
  isConnected: boolean;
  wsStatus: WsStatus;
  backoffMs: number;
}

const initialState: ConnectionState = {
  isConnected: false,
  wsStatus: "idle",
  backoffMs: 1000,
};

// --- Slice ---

const connectionSlice = createSlice({
  name: "connection",
  initialState,
  reducers: {
    wsConnecting(state) {
      state.wsStatus = "connecting";
      state.isConnected = false;
    },
    wsConnected(state) {
      state.wsStatus = "connected";
      state.isConnected = true;
      state.backoffMs = 1000;
    },
    wsDisconnected(state, action: PayloadAction<{ backoffMs: number }>) {
      state.wsStatus = "disconnected";
      state.isConnected = false;
      state.backoffMs = action.payload.backoffMs;
    },
    wsReconnecting(state) {
      state.wsStatus = "reconnecting";
      state.isConnected = false;
    },
  },
});

export const { wsConnecting, wsConnected, wsDisconnected, wsReconnecting } =
  connectionSlice.actions;
export default connectionSlice.reducer;

// --- Selectors ---
// Use local state shape to avoid circular dep with store/index.ts

type ConnectionRootState = { connection: ConnectionState };

export const selectIsConnected = (state: ConnectionRootState) =>
  state.connection.isConnected;
export const selectWsStatus = (state: ConnectionRootState) =>
  state.connection.wsStatus;
