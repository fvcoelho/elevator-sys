import {
  createSlice,
  createSelector,
  createAsyncThunk,
} from "@reduxjs/toolkit";
import type { PayloadAction } from "@reduxjs/toolkit";
import type {
  Passenger,
  ReturnTripRequest,
  ElevatorDto,
} from "@/types/elevator";

// --- Module-level abort controllers for return trip timers ---
// Kept outside Redux state because AbortController is not serializable

const returnTripAbortControllers = new Map<number, AbortController>();

// --- Helpers (ported from use-passengers.ts) ---

const DROPOFF_STATES = new Set([
  "DOOR_OPEN",
  "DOOR_OPENING",
  "DOOR_CLOSING",
  "IDLE",
]);

function findBestElevator(
  elevators: ElevatorDto[],
  floor: number,
  destinationFloor: number,
  assignedCount: Map<number, number>
): ElevatorDto | undefined {
  const candidates = elevators.filter(
    (e) => e.currentFloor === floor && e.targetFloors.includes(destinationFloor)
  );
  if (candidates.length === 0) return undefined;

  return candidates.reduce((a, b) =>
    (assignedCount.get(a.index) || 0) <= (assignedCount.get(b.index) || 0)
      ? a
      : b
  );
}

function checkArrival(
  passenger: Passenger,
  elevators: ElevatorDto[]
): boolean {
  const { elevatorIndex, destinationFloor } = passenger;

  if (elevatorIndex !== undefined) {
    const myElevator = elevators.find((e) => e.index === elevatorIndex);
    return myElevator
      ? myElevator.currentFloor === destinationFloor &&
          DROPOFF_STATES.has(myElevator.state)
      : false;
  }

  return elevators.some(
    (e) => e.currentFloor === destinationFloor && DROPOFF_STATES.has(e.state)
  );
}

// --- State ---

interface PassengersState {
  passengers: Passenger[];
  nextId: number;
  returnQueue: ReturnTripRequest[];
}

// Always start empty — localStorage is loaded client-side after mount
// to avoid SSR/hydration mismatch (server has no localStorage).
const initialState: PassengersState = {
  passengers: [],
  nextId: 1,
  returnQueue: [],
};

// --- Thunk: schedule return trip ---

export const scheduleReturnTrip = createAsyncThunk<
  void,
  Passenger
>(
  "passengers/scheduleReturnTrip",
  async (passenger, { dispatch, signal }) => {
    const controller = new AbortController();
    returnTripAbortControllers.set(passenger.id, controller);

    // Link the thunk's signal to our controller
    signal.addEventListener("abort", () => {
      controller.abort();
    });

    await new Promise<void>((resolve) => {
      const timer = setTimeout(
        resolve,
        (passenger.returnDelaySec ?? 0) * 1000
      );
      controller.signal.addEventListener("abort", () => {
        clearTimeout(timer);
        resolve();
      });
    });

    returnTripAbortControllers.delete(passenger.id);

    if (!controller.signal.aborted) {
      dispatch(
        passengerStatusUpdated({
          id: passenger.id,
          updates: {
            status: "returning",
            pickupFloor: passenger.destinationFloor,
            destinationFloor: 1,
            currentFloor: passenger.destinationFloor,
          },
        })
      );
      dispatch(
        returnQueueItemAdded({
          name: passenger.name,
          fromFloor: passenger.destinationFloor,
        })
      );
    }
  }
);

// --- Slice ---

const passengersSlice = createSlice({
  name: "passengers",
  initialState,
  reducers: {
    passengerAdded(
      state,
      action: PayloadAction<{
        name: string;
        pickupFloor: number;
        destinationFloor: number;
        returnDelaySec?: number;
        requestId?: number;
      }>
    ) {
      state.passengers.push({
        id: state.nextId++,
        ...action.payload,
        status: "waiting",
        currentFloor: action.payload.pickupFloor,
      });
    },

    passengersCleared(state) {
      // Cancel all pending return trip timers
      for (const controller of returnTripAbortControllers.values()) {
        controller.abort();
      }
      returnTripAbortControllers.clear();

      state.passengers = [];
      state.returnQueue = [];
    },

    // Pure sync: updates passenger statuses based on current elevator positions.
    // Returns list of newly arrived passenger IDs (for middleware to schedule return trips).
    passengersSync(state, action: PayloadAction<ElevatorDto[]>) {
      const elevators = action.payload;
      let changed = false;

      const assignedCount = new Map<number, number>();
      for (const p of state.passengers) {
        if (p.status === "riding" && p.elevatorIndex !== undefined) {
          assignedCount.set(
            p.elevatorIndex,
            (assignedCount.get(p.elevatorIndex) || 0) + 1
          );
        }
      }

      for (let i = 0; i < state.passengers.length; i++) {
        const p = state.passengers[i];

        // Waiting or returning → try to board
        if (p.status === "waiting" || p.status === "returning") {
          const elevator = findBestElevator(
            elevators,
            p.currentFloor,
            p.destinationFloor,
            assignedCount
          );
          if (elevator) {
            changed = true;
            assignedCount.set(
              elevator.index,
              (assignedCount.get(elevator.index) || 0) + 1
            );
            state.passengers[i] = {
              ...p,
              status: "riding",
              elevatorIndex: elevator.index,
            };
            continue;
          }
        }

        // Riding → check arrival
        if (p.status === "riding" && checkArrival(p, elevators)) {
          changed = true;
          const isLobby = p.destinationFloor === 1;
          state.passengers[i] = {
            ...p,
            status: "arrived",
            currentFloor: p.destinationFloor,
            arrivedAt: isLobby ? undefined : Date.now(),
            returnDelaySec: isLobby ? undefined : p.returnDelaySec,
            elevatorIndex: undefined,
          };
        }
      }

      // Immer requires we signal a change if we want React to re-render.
      // The mutation above handles it automatically (Immer tracks mutations).
      void changed;
    },

    returnQueueConsumed(state) {
      state.returnQueue = [];
    },

    returnQueueItemAdded(state, action: PayloadAction<ReturnTripRequest>) {
      state.returnQueue.push(action.payload);
    },

    passengerStatusUpdated(
      state,
      action: PayloadAction<{
        id: number;
        updates: Partial<
          Pick<
            Passenger,
            | "status"
            | "pickupFloor"
            | "destinationFloor"
            | "currentFloor"
            | "elevatorIndex"
          >
        >;
      }>
    ) {
      const idx = state.passengers.findIndex((p) => p.id === action.payload.id);
      if (idx !== -1) {
        Object.assign(state.passengers[idx], action.payload.updates);
      }
    },

    hydratePassengers(
      state,
      action: PayloadAction<{ passengers: Passenger[]; nextId: number }>
    ) {
      // Only hydrate if no passengers have been added yet
      if (state.passengers.length === 0) {
        state.passengers = action.payload.passengers;
        state.nextId = action.payload.nextId;
      }
    },
  },
});

export const {
  passengerAdded,
  passengersCleared,
  passengersSync,
  returnQueueConsumed,
  returnQueueItemAdded,
  passengerStatusUpdated,
  hydratePassengers,
} = passengersSlice.actions;

export default passengersSlice.reducer;

// --- Selectors ---
// Use local state shape to avoid circular dep with store/index.ts

type PassengersRootState = { passengers: PassengersState };

export const selectPassengers = (state: PassengersRootState) =>
  state.passengers.passengers;
export const selectReturnQueue = (state: PassengersRootState) =>
  state.passengers.returnQueue;

export const selectTotalPeople = createSelector(
  selectPassengers,
  (passengers) => passengers.length
);

export const selectWaitingLobby = createSelector(
  selectPassengers,
  (passengers) =>
    passengers.filter((p) => p.status === "waiting" && p.currentFloor === 1)
      .length
);

export const selectPassengersByFloor = (floor: number) =>
  createSelector(selectPassengers, (passengers) =>
    passengers.filter(
      (p) =>
        p.currentFloor === floor &&
        (p.status === "waiting" ||
          p.status === "arrived" ||
          p.status === "returning")
    )
  );

export const selectRidingByElevator = (index: number) =>
  createSelector(selectPassengers, (passengers) =>
    passengers.filter(
      (p) => p.status === "riding" && p.elevatorIndex === index
    )
  );

// Export helpers for use in middleware
export { findBestElevator, checkArrival };
