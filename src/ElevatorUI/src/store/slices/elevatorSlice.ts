import { createSlice, createSelector } from "@reduxjs/toolkit";
import type { PayloadAction } from "@reduxjs/toolkit";
import type { SystemStatusDto } from "@/types/elevator";

// --- State ---

interface ElevatorState {
  status: SystemStatusDto | null;
  messageCount: number;
}

// Always start empty — localStorage is loaded client-side after mount
// to avoid SSR/hydration mismatch (server has no localStorage).
const initialState: ElevatorState = {
  status: null,
  messageCount: 0,
};

// --- Slice ---

const elevatorSlice = createSlice({
  name: "elevator",
  initialState,
  reducers: {
    statusReceived(state, action: PayloadAction<SystemStatusDto>) {
      state.status = action.payload;
      state.messageCount += 1;
    },
    statusCleared(state) {
      state.status = null;
      state.messageCount = 0;
    },
    hydrateElevator(state, action: PayloadAction<{ status: SystemStatusDto | null }>) {
      // Only hydrate if no live data has arrived yet
      if (state.status === null) {
        state.status = action.payload.status;
      }
    },
  },
});

export const { statusReceived, statusCleared, hydrateElevator } = elevatorSlice.actions;
export default elevatorSlice.reducer;

// --- Selectors ---
// Use local state shape to avoid circular dep with store/index.ts

type ElevatorRootState = { elevator: ElevatorState };

export const selectStatus = (state: ElevatorRootState) => state.elevator.status;
export const selectMessageCount = (state: ElevatorRootState) =>
  state.elevator.messageCount;

export const selectElevators = createSelector(
  selectStatus,
  (status) => status?.elevators ?? []
);

export const selectAlgorithm = createSelector(
  selectStatus,
  (status) => status?.algorithm ?? "Custom"
);

export const selectIsEmergencyStopped = createSelector(
  selectStatus,
  (status) => status?.isEmergencyStopped ?? false
);

// Re-export state type alias for use in other files
export type { ElevatorRootState };
