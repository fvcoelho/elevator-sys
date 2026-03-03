import { configureStore, combineReducers } from "@reduxjs/toolkit";
import type { AnyAction } from "@reduxjs/toolkit";
import elevatorReducer from "./slices/elevatorSlice";
import connectionReducer from "./slices/connectionSlice";
import passengersReducer from "./slices/passengersSlice";
import timelineReducer, {
  replaySnapshot,
  timelineInitialState,
} from "./slices/timelineSlice";
import { websocketMiddleware } from "./middleware/websocketMiddleware";
import { timelineMiddleware } from "./middleware/timelineMiddleware";
import { setupLocalStorageSubscriber } from "./subscribers/localStorageSubscriber";

const appReducer = combineReducers({
  elevator: elevatorReducer,
  connection: connectionReducer,
  passengers: passengersReducer,
  timeline: timelineReducer,
});

type AppState = ReturnType<typeof appReducer>;

// Wrapped reducer handles two special cases:
//   1. replaySnapshot: replaces elevator/connection/passengers with a recorded snapshot
//   2. During replay: blocks live WS updates so the replayed state isn't overwritten
const rootReducer = (
  state: AppState | undefined,
  action: AnyAction
): AppState => {
  if (replaySnapshot.match(action)) {
    return {
      elevator: action.payload.elevator,
      connection: action.payload.connection,
      passengers: action.payload.passengers,
      timeline: state?.timeline ?? timelineInitialState,
    };
  }

  // Block live updates during replay so the scrubbed state is preserved
  if (state?.timeline.isReplaying) {
    const BLOCKED = ["elevator/statusReceived", "passengers/passengersSync"];
    if (BLOCKED.includes(action.type)) return state;
  }

  return appReducer(state, action);
};

export const store = configureStore({
  reducer: rootReducer,
  middleware: (getDefaultMiddleware) =>
    getDefaultMiddleware({
      // Snapshots can be large — skip RTK's serializable/immutability checks on them
      serializableCheck: {
        ignoredActions: ["timeline/snapshotCaptured", "timeline/replaySnapshot"],
      },
      immutabilityCheck: {
        ignoredActions: ["timeline/snapshotCaptured", "timeline/replaySnapshot"],
      },
    }).concat(websocketMiddleware, timelineMiddleware),
});

// Set up localStorage persistence (only in browser)
if (typeof window !== "undefined") {
  setupLocalStorageSubscriber(store);
}

export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;
